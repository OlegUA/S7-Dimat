﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharp7;

namespace S7_Dimat.Class
{
    class Plc
    {
        private string _IP;
        private int _Rack;
        private int _Slot;
        private S7Type PlcType;

        private S7Client client;
        public enum S7Type{ S7300, S7400, S71200, S71500 }

        public Boolean Connected
        {
            get
            {
                return client.Connected;
            }
        }

        public S7Type Type
        {
            set
            {
                PlcType = value;
            }

            get
            {
                return PlcType;
            }
        }

        public Plc(string Adrress, S7Type Plc)
        {
            PlcType = Plc;
            switch (Plc)
            {
                case S7Type.S7300:
                    SetPlc(Adrress, 0, 2);
                    break;
                case S7Type.S7400:
                case S7Type.S71200:
                case S7Type.S71500:
                    SetPlc(Adrress, 0, 0);
                    break;
            }
        }

        public Plc(string Adrress, int Rack, int Slot)
        {
            // Set minimum values
            Rack = Rack < 0 ? 0 : Rack;
            Slot = Slot < 1 ? 1 : Slot;
            // Set max values
            Rack = Rack > 7 ? 7 : Rack;
            Slot = Slot > 31 ? 31 : Slot;

            SetPlc(Adrress, Rack, Slot);
        }

        private void SetPlc(string Adrress, int Rack, int Slot)
        {
            this._IP = Adrress;
            this._Rack = Rack;
            this._Slot = Slot;

            this.client = new S7Client();
        }

        public Boolean Connect()
        {
            this.Disconnect();

            if (client.ConnectTo(_IP, _Rack, _Slot) == 0)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public void Disconnect()
        {
            client.Disconnect();
        }

        public string GetValue(string Value)
        {
            Boolean Input = Value.StartsWith("I");
            Boolean Output = Value.StartsWith("Q");
            Boolean M = Value.StartsWith("M");
            Boolean DB = Value.StartsWith("DB");

            int Area = 0;
            int BufferSize;
            int Amount = 1;
            int Start = 0;
            int BitPosition;
            int DBNumber = 0;

            // Set type of variable to read
            if (Input) { Area = S7Consts.S7AreaPE; }
            if (Output) { Area = S7Consts.S7AreaPA; }
            if (M) { Area = S7Consts.S7AreaMK; }
            if (DB) { Area = S7Consts.S7AreaDB; }

            // Do I read bit?
            Boolean ReadBit = ReadingBit(DB, Value);

            // Set Buffer Size
            BufferSize = SetBufferSize(ReadBit, DB, Value);

            // Start adrress
            Start = SetStartAdrress(ReadBit, DB, Value);

            // Bit position
            if (ReadBit) { 
                BitPosition = SetBitPosition(DB, Value);
                Start = Start + BitPosition;
            }

            // Set DBnummber
            if (DB) { 
                DBNumber = SetDBnummber(Value);
            }

            byte[] buffer = new byte[BufferSize];

            buffer = GetBuffer(Area, BufferSize, DBNumber, Start, Amount, ReadBit);

            return S7.GetBitAt(buffer, 0, 0).ToString();

        }

        public void test()
        {
            byte[] buffer = new byte[1];
            client.ReadArea(S7Consts.S7AreaPE, 0, 15, 1, S7Consts.S7WLBit, buffer);
            Boolean b = S7.GetBitAt(buffer, 0, 5);

        }


        private byte[] GetBuffer(int S7Area, int BufferSize, int DBNumber, int Start, int Amount, Boolean ReadBit)
        {
            int WordLen = 0;
            if (ReadBit)
            {
                WordLen = S7Consts.S7WLBit;
            } else
            {
                WordLen = BufferSize == 1 ? S7Consts.S7WLByte : 0;
                WordLen = BufferSize == 2 ? S7Consts.S7WLWord : WordLen;
                WordLen = BufferSize == 4 ? S7Consts.S7WLDWord : WordLen;
            }
            
            byte[] buffer = new byte[BufferSize];
            client.ReadArea(S7Area, DBNumber, Start, Amount, WordLen, buffer);
            return buffer;
        }

        private int SetDBnummber(string Value)
        {
                return Int32.Parse(Value.Split('.')[0].Substring(2));

        }

        private int SetBitPosition(Boolean DB, string Value)
        {
            if (!DB)
            {
                    return Int32.Parse(Value.Split('.')[1]);
            }else
            {
                    return Int32.Parse(Value.Split('.')[2]);
            }
        }

        private int SetStartAdrress(Boolean ReadBit, Boolean DB, string Value)
        {
            if (!ReadBit)
            {
                if (!DB)
                {
                    return Int32.Parse(Value.Substring(2));
                }
                else
                {
                    return Int32.Parse(Value.Split('.')[1].Substring(3));
                }

            }
            else
            {
                // Start adrress for reading bit
                if (!DB)
                {
                    return (Int32.Parse(Value.Split('.')[0].Substring(1)) * 8);
                }
                else
                {
                    return (Int32.Parse(Value.Split('.')[1].Substring(3)) * 8);
                }
            }
        }

        private int SetBufferSize(Boolean ReadBit, Boolean DB, string Value)
        {
            int val = 0;

            if (ReadBit)
            {
                val = 1;
            } else {

                string ReadLetter;

                if (!DB)
                {
                    // IB, IW, ID
                    ReadLetter = Value.Substring(1, 1);
                }
                else
                {
                    // DB100.DB[B]1
                    ReadLetter = Value.Split('.')[1].Substring(2, 1);
                }

                if (ReadLetter == "B") { val = 1; }
                if (ReadLetter == "W") { val = 2; }
                if (ReadLetter == "D") { val = 4; }
            }

            return val;
        }

        private Boolean ReadingBit(Boolean DB, string Value)
        {
            if (!DB && Value.Contains("."))
            {
                return true;
            }

            if (DB)
            {
                if (Value.Split('.').Length > 2)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
