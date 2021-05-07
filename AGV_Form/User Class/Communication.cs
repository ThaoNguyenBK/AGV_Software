﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;
using System.Timers;

namespace AGV_Form
{
    public delegate void SerialConnectionHander(bool open);
    class Communication
    {

        //public event SerialConnectionHander IsConnected;
        private static SerialPort _serialPort = new SerialPort();
        public static SerialPort SerialPort
        {
            get { return _serialPort; }
            set
            {

                _serialPort = value;
            }

        }
        private static List<byte> bytesReceived = new List<byte>();
        private const ushort PIDInfoReceivePacketSize = 24;
        private const ushort PathCompleteReceivePacketSize = 7;
        public static byte currentNode;
        public static char currentOrient;
        public static float speed = 0, line = 0;
        public static int com = 0;
        public static Int16 countToSendPath2 = 0;
        public static System.Timers.Timer timerToSendPath2Again;
        //  public static System.Windows.Forms.Timer timerToSendPath2Again;
        public static void GetDataRecieve()
        {

            int rxBufferSize = 22;
            byte[] rxBuffer = new byte[rxBufferSize];
            int rxByteCount = Communication.SerialPort.Read(rxBuffer, 0, rxBufferSize);
            // add to a list of bytes received
            for (int i = 0; i < rxByteCount; i++) bytesReceived.Add(rxBuffer[i]);

            int startIndex = 0;
            byte functionCode = new byte();

            // check header
            if (bytesReceived.Count < 3) return;
            for (int i = 0; i < bytesReceived.Count - 3; i++)
            {
                if (bytesReceived[i] == 0xAA && bytesReceived[i + 1] == 0xFF)
                {
                    startIndex = i;
                    functionCode = bytesReceived[i + 2];

                    if (functionCode == 0xA0)
                    {
                        // waitting for receive enough frame data of this function code
                        if (bytesReceived.Count - startIndex < PIDInfoReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PIDInfoReceivePacketSize];
                        for (int j = 0; j < PIDInfoReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PIDInfoReceivePacket receiveFrame = PIDInfoReceivePacket.FromArray(data);

                        // check sum
                        //   ushort crc = 0;
                        //   for (int j = 0; j < PIDInfoReceivePacketSize - 4; j++)
                        //      crc += data[j];
                        // if (crc != receiveFrame.CheckSum) continue;

                        bytesReceived.RemoveRange(0, startIndex + PIDInfoReceivePacketSize - 1);

                        // update AGV info to lists of AGVs (real-time mode)
                        if (receiveFrame.Header[0] == 0xAA && receiveFrame.Header[1] == 0xFF && receiveFrame.EndOfFrame[0] == 0x0D && receiveFrame.EndOfFrame[1] == 0x0A)
                        {
                            speed = receiveFrame.Velocity;
                            //dk_Speed = receiveFrame.UdkVelocity;
                            line = receiveFrame.LinePos;
                            //udk_LinePos = receiveFrame.UdkLinePos;
                            currentNode = receiveFrame.CurrentNode;
                            currentOrient = receiveFrame.currentOrient;

                        }
                    }
                    else if (functionCode == 0xC0)      //Xac nhan chay xong Path
                    {
                        //com++;
                        // waitting for receive enough frame data of this function code
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);
                        //if(receiveFrame.IsComplete == 0x05)
                        // {

                        //}
                        //string fullpath = "P-2-S-53-N-49-W-44-N-37-G-D-3";
                        //Communication.SendPathData(fullpath);

                        timerToSendPath2Again = new System.Timers.Timer(50);
                        timerToSendPath2Again.Elapsed += timerToSendPath2Again_Elapsed;
                        // timerToSendPath2Again.Interval = 50;
                        timerToSendPath2Again.AutoReset = true;
                        countToSendPath2 = 10;
                        sendPath2Frame();

                        //  timerToSendPath2Again.Enabled = true;

                        //label20.Text = AGV.FullPathOfAGV[agv.ID];
                        // timerToSendPath2Again = new System.Windows.Forms.Timer();



                        //  agv.Path.Clear();
                        //AGV.ListAGV[0].Tasks.Remove(currentTask);  

                    }
                    else if (functionCode == 0xC1)             //ACK Speed Frame
                    {
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);

                        if (DashboardForm.timerToSendSpeedAgain.Enabled == true)
                            DashboardForm.timerToSendSpeedAgain.Stop();
                    }
                    else if (functionCode == 0xC2)             //ACK Line Frame
                    {
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);

                        if (DashboardForm.timerToSendLineAgain.Enabled == true)
                            DashboardForm.timerToSendLineAgain.Stop();
                    }
                    else if (functionCode == 0xC3)             //ACK Path Frame
                    {
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);

                        countToSendPath2 = 0;
                        if (DashboardForm.timerToSendPathAgain.Enabled == true)
                            DashboardForm.timerToSendPathAgain.Stop();
                        else if (timerToSendPath2Again.Enabled == true)
                            timerToSendPath2Again.Stop();
                    }
                    else if (functionCode == 0xD0)                  // NAK Speed Frame
                    {
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);

                        DashboardForm.timerToSendSpeedAgain.Enabled = false;
                        DashboardForm.sendSpeedFrame();
                    }
                    else if (functionCode == 0xD1)                  // NAK Line Frame
                    {
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);

                        DashboardForm.timerToSendLineAgain.Enabled = false;
                        DashboardForm.sendLineFrame();
                    }
                    else if (functionCode == 0xD2)                  // NAK Path Frame
                    {
                        if (bytesReceived.Count - startIndex < PathCompleteReceivePacketSize) return;

                        // put data in an array
                        byte[] data = new byte[PathCompleteReceivePacketSize];
                        for (int j = 0; j < PathCompleteReceivePacketSize; j++)
                            data[j] = bytesReceived[startIndex + j];

                        PathCompleteRecievePacket receiveFrame = PathCompleteRecievePacket.FromArray(data);
                        bytesReceived.RemoveRange(0, startIndex + PathCompleteReceivePacketSize - 1);

                        countToSendPath2++;

                        if (DashboardForm.timerToSendPathAgain.Enabled == true)
                        {
                            DashboardForm.timerToSendPathAgain.Enabled = false;
                            DashboardForm.sendPathFrame();
                        }
                        else if (timerToSendPath2Again.Enabled == true)
                        {
                            timerToSendPath2Again.Enabled = false;
                            sendPath2Frame();
                        }

                    }

                }
            }
        }

        public static void sendPath2Frame()
        {
            AGV agv = AGV.ListAGV[0];
            string pick, drop;
            if (AGV.ListAGV[0].Tasks.Count == 0) return;
            Task currentTask = AGV.ListAGV[0].Tasks[0];

            if (currentTask.PickLevel == 1 || currentTask.PickLevel == 2 || currentTask.PickLevel == 3)
            {
                pick = "P-" + currentTask.PickLevel.ToString() + "-";
            }
            else
            {
                pick = "N-0-";
            }
            if (currentTask.DropLevel == 1 || currentTask.DropLevel == 2 || currentTask.DropLevel == 3)
            {
                drop = "-D-" + currentTask.DropLevel.ToString();
            }
            else
            {
                drop = "-N-0";
            }
            try
            {
                AGV.FullPathOfAGV[agv.ID] = pick + agv.CurrentOrient.ToString() + "-" + Navigation.GetNavigationFrame(agv.Path[1], Node.MatrixNodeOrient) + drop;
                Communication.SendPathData(AGV.FullPathOfAGV[agv.ID]);

                if (countToSendPath2 == 11)
                {
                    agv.Path.Clear();
                    AGV.ListAGV[0].Tasks.Remove(currentTask);
                    countToSendPath2 = 0;
                }

            }
            catch { }

            if (timerToSendPath2Again.Enabled == false)
                timerToSendPath2Again.Start();

        }

        // private static void timerToSendPath2Again_Tick(Object source, System.Timers.ElapsedEventArgs e)
        private static void timerToSendPath2Again_Elapsed(Object source, System.Timers.ElapsedEventArgs e)
        {
            timerToSendPath2Again.Stop();
            sendPath2Frame();
        }

        public static void SendPathData(string fullpath)
        {
            PathInfoSendPacket sendFrame = new PathInfoSendPacket();


            //string fullpath = "N,0,S,11,N,3,W,0,S,42,E,46,G,N,0";
            string[] path = fullpath.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] arrPathFrame = new byte[path.Length];
            int sendBytecount = path.Length;
            arrPathFrame[0] = (byte)path[0][0];
            arrPathFrame[1] = Convert.ToByte(path[1]);
            arrPathFrame[2] = (byte)path[2][0];
            arrPathFrame[sendBytecount - 2] = (byte)path[sendBytecount - 2][0];
            arrPathFrame[sendBytecount - 1] = Convert.ToByte(path[sendBytecount - 1]);
            for (int i = 3; i < sendBytecount - 2; i++)
            {
                if (i % 2 == 0) arrPathFrame[i] = (byte)path[i][0];
                else
                {
                    arrPathFrame[i] = Convert.ToByte(path[i]);
                }


            }
            // set frame to send as struct
            // note: send reversed Header and End-of-frame because of Intel processors (in my laptop) use little endian
            sendFrame.Header = new byte[2] { 0xAA, 0xFF };
            sendFrame.FunctionCode = 0xA1;
            sendFrame.AGVID = 0x01;
            sendFrame.PathByteCount = Convert.ToByte(sendBytecount);
            sendFrame.Path = arrPathFrame;
            sendFrame.CheckSum = new byte[2];
            CRC16_Calculator(sendFrame.ToArrayCRC(), sendFrame.CheckSum);
            //sendFrame.CheckSum = 0xFFFF;
            sendFrame.EndOfFrame = new byte[2] { 0x0A, 0x0D };
            try { Communication.SerialPort.Write(sendFrame.ToArray(), 0, sendFrame.ToArray().Length); }
            catch { };
            // wait ack

        }

        public static void CRC16_Calculator(byte[] messageArray, byte[] CRC)
        {
            ushort CRCFull = 0xFFFF;
            char CRCLSB;

            for (int i = 0; i < (messageArray.Length); i++)
            {
                CRCFull = (ushort)(CRCFull ^ messageArray[i]);

                for (int j = 0; j < 8; j++)
                {
                    CRCLSB = (char)(CRCFull & 0x0001);
                    CRCFull = (ushort)((CRCFull >> 1) & 0x7FFF);

                    if (CRCLSB == 1)
                        CRCFull = (ushort)(CRCFull ^ 0xA001);
                }
            }
            CRC[0] = (byte)(CRCFull & 0xFF);
            CRC[1] = (byte)((CRCFull >> 8) & 0xFF);


        }


        /* Path information packet (send):
    * Header		    2 byte  -> 0xFFAA
    * FunctionCode	    1 byte  -> 0xA1
    * AGVID 		    1 byte
    * Path Byte Count  1 byte
    * Path             (dynamic)
    * CheckSum	        2 byte  -> sum of bytes from Header to Path
    * EndOfFrame	    2 byte  -> 0x0D0A
    */
        public struct PathInfoSendPacket
        {
            public byte[] Header;
            public byte FunctionCode;
            public byte AGVID;
            public byte PathByteCount;
            public byte[] Path;
            public byte[] CheckSum;
            public byte[] EndOfFrame;

            // Convert Structs to Byte Arrays
            public byte[] ToArray()
            {
                var stream = new System.IO.MemoryStream();
                var writer = new System.IO.BinaryWriter(stream);

                writer.Write(this.Header);
                writer.Write(this.FunctionCode);
                writer.Write(this.AGVID);
                writer.Write(this.PathByteCount);
                writer.Write(this.Path);
                writer.Write(this.CheckSum);
                writer.Write(this.EndOfFrame);

                return stream.ToArray();
            }
            public byte[] ToArrayCRC()
            {
                var stream1 = new System.IO.MemoryStream();
                var writer1 = new System.IO.BinaryWriter(stream1);


                writer1.Write(this.FunctionCode);
                writer1.Write(this.AGVID);
                writer1.Write(this.PathByteCount);
                writer1.Write(this.Path);

                //   writer.Write(this.CheckSum);
                return stream1.ToArray();
            }
        }

        public struct SetSpeedPacket
        {
            public byte[] Header;
            public byte FunctionCode;
            public byte AGVID;
            public float Kp;
            public float Ki;
            public float Kd;
            public float Velocity;
            public byte[] CheckSum;
            //  public ushort CheckSum;
            public byte[] EndOfFrame;

            // Convert Structs to Byte Arrays
            public byte[] ToArray()
            {
                var stream = new System.IO.MemoryStream();
                var writer = new System.IO.BinaryWriter(stream);

                writer.Write(this.Header);
                writer.Write(this.FunctionCode);
                writer.Write(this.AGVID);
                writer.Write(this.Kp);
                writer.Write(this.Ki);
                writer.Write(this.Kd);
                writer.Write(this.Velocity);
                writer.Write(this.CheckSum);
                //   writer.Write(this.CheckSum);
                writer.Write(this.EndOfFrame);

                return stream.ToArray();
            }
            public byte[] ToArrayCRC()
            {
                var stream1 = new System.IO.MemoryStream();
                var writer1 = new System.IO.BinaryWriter(stream1);


                writer1.Write(this.FunctionCode);
                writer1.Write(this.AGVID);
                writer1.Write(this.Kp);
                writer1.Write(this.Ki);
                writer1.Write(this.Kd);
                writer1.Write(this.Velocity);
                //   writer.Write(this.CheckSum);

                return stream1.ToArray();
            }
        }
        public struct SetLinePosPacket
        {
            public byte[] Header;
            public byte FunctionCode;
            public byte AGVID;
            public float Kp;
            public float Ki;
            public float Kd;
            public byte[] CheckSum;
            // public float unNecessary;
            public byte[] EndOfFrame;
            // Convert Structs to Byte Arrays
            public byte[] ToArray()
            {
                var stream = new System.IO.MemoryStream();
                var writer = new System.IO.BinaryWriter(stream);

                writer.Write(this.Header);
                writer.Write(this.FunctionCode);
                writer.Write(this.AGVID);
                writer.Write(this.Kp);
                writer.Write(this.Ki);
                writer.Write(this.Kd);
                writer.Write(this.CheckSum);
                //    writer.Write(this.unNecessary);
                //  writer.Write(this.CheckSum);
                writer.Write(this.EndOfFrame);

                return stream.ToArray();
            }
            public byte[] ToArrayCRC()
            {
                var stream1 = new System.IO.MemoryStream();
                var writer1 = new System.IO.BinaryWriter(stream1);


                writer1.Write(this.FunctionCode);
                writer1.Write(this.AGVID);
                writer1.Write(this.Kp);
                writer1.Write(this.Ki);
                writer1.Write(this.Kd);
                //   writer.Write(this.CheckSum);


                return stream1.ToArray();
            }
        }
        public struct PIDInfoReceivePacket
        {
            public byte[] Header;
            public byte FunctionCode;
            public byte AGVID;
            public float Velocity;
            public float UdkVelocity;
            public float LinePos;
            public float UdkLinePos;
            public byte CurrentNode;
            public char currentOrient;
            public byte[] EndOfFrame;


            // Convert Byte Arrays to Structs
            public static PIDInfoReceivePacket FromArray(byte[] bytes)
            {
                var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(bytes));

                var s = default(PIDInfoReceivePacket);


                s.Header = reader.ReadBytes(2);
                s.FunctionCode = reader.ReadByte();
                s.AGVID = reader.ReadByte();

                s.Velocity = reader.ReadSingle();
                s.UdkVelocity = reader.ReadSingle();
                s.LinePos = reader.ReadSingle();
                s.UdkLinePos = reader.ReadSingle();
                s.CurrentNode = reader.ReadByte();
                s.currentOrient = reader.ReadChar();

                s.EndOfFrame = reader.ReadBytes(2);

                // s.CheckSum = reader.ReadUInt16();
                //  s.EndOfFrame = reader.ReadBytes(2);

                return s;
            }
        }

        public struct PathCompleteRecievePacket
        {
            public byte[] Header;
            public byte FunctionCode;
            public byte AGVID;
            public byte IsComplete;
            public byte[] EndOfFrame;


            // Convert Byte Arrays to Structs
            public static PathCompleteRecievePacket FromArray(byte[] bytes)
            {
                var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(bytes));

                var s = default(PathCompleteRecievePacket);


                s.Header = reader.ReadBytes(2);
                s.FunctionCode = reader.ReadByte();
                s.AGVID = reader.ReadByte();

                s.IsComplete = reader.ReadByte();

                s.EndOfFrame = reader.ReadBytes(2);

                // s.CheckSum = reader.ReadUInt16();
                //  s.EndOfFrame = reader.ReadBytes(2);

                return s;
            }
        }
    }
}
