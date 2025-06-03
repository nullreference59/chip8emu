using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu {

    public interface IEmulator {
        void Reset();
        void ExecuteCycle();

        bool[,] Graphics { get; }
    }

    public class Emulator : IEmulator {
        private byte[] memory = new byte[4096];
        public bool[,] Graphics { get; private set; }
        private ushort PC = 0;
        private ushort I = 0;
        private Stack<ushort> stack = new Stack<ushort>();

        private ushort DelayTimer = 0;
        private ushort SoundTimer = 0;

        public IDisplay? Display { get; set; }

        private byte[] V = new byte[16];

        public Emulator() {
            Graphics = new bool[Constants.Width, Constants.Height];
            LoadFonts();
        }

        public void LoadRom(byte[] rom) {
            rom.CopyTo(memory, Constants.RomStart);
        }

        private void LoadFonts() {
            Constants.Fonts.CopyTo(memory, 0x0);
        }

        public void Reset() {
            PC = Constants.RomStart;
            I = 0;
            stack.Clear();
            Graphics = new bool[Constants.Width, Constants.Height];
        }

        public void ExecuteCycle() {

            //Fetch + Decode
            var instruction = GetDecodedInstruction(memory[PC], memory[PC + 1]);
                
            PC += 2;

            //Execute
            ExecuteInstruction(instruction);
        }

        private Instruction GetDecodedInstruction(byte first, byte second) {

            // Extract Op codes
            var firstHex = first.ToString("X");
            var secondHex = second.ToString("X");
            var opcode = (ushort)(first << 8 | second);
            // Pull two bytes off of memory, PC and PC+1, then increment the Program Counter by 2.

            var instruction = new Instruction() {
                OpCode = opcode,
                // Second Nibble
                X = (byte)((opcode & 0x0F00) >> 8),
                // Third Nibble
                Y = (byte)((opcode & 0x00F0) >> 4),
                // Fourth Nibble
                N = (byte)(opcode & 0x000F),
                //Second Byte
                NN = (byte)(opcode & 0x00FF),
                // Second, Third, Fourth Nibble (mem address)
                NNN = (ushort)(opcode & 0x0FFF)

            };

            return instruction;
        }

        private void ExecuteInstruction(Instruction instruction) {
            var hexOpCode = instruction.OpCode.ToString("X");
            //Switch off of the first nibble of the OpCode
             var inst = instruction.OpCode & 0xF000;
            switch(inst) {
                case 0x0000 when instruction.OpCode == 0x00E0:
                    OpCode00E0();
                    break;
                case 0x0000:
                    // Execute Machine Code at 12 bit address (Not Supported)
                    OpCodeONNN(instruction);
                    break;
                case 0x00E0:
                    // Clear Screen
                    OpCode00E0();
                    break;
                case 0x1000:
                    //Set PC to NNN
                    OpCode1NNN(instruction);
                    break;
                case 0x6000:
                    //Set V[X] to NN
                    OpCode6XNN(instruction);
                    break;
                case 0x7000:
                    //Add NN to V[X]
                    OpCode7XNN(instruction);
                    break;
                case 0xA000:
                    //Set index register to NNN
                    OpCodeANNN(instruction);
                    break;
                case 0xD000:
                    //Display/Draw
                    OpCodeDXYN(instruction);
                    break;
                default:
                    throw new Exception("Invalid instruction encountered");
            }
        }

        private void OpCodeONNN(Instruction instruction) {
            throw new NotImplementedException("Emulator does not support 0NNN instructions");
        }

        private void OpCode00E0() {
            Graphics = new bool[Constants.Width, Constants.Height];
        }


        private void OpCode1NNN(Instruction instruction) {
            PC = instruction.NNN;
        }

        private void OpCodeANNN(Instruction instruction) {
            I = instruction.NNN;
        }

        private void OpCode7XNN(Instruction instruction) {
            V[instruction.X] += instruction.NN;
        }

        private void OpCode6XNN(Instruction instruction) {
            V[instruction.X] = instruction.NN;
        }

        // Source: https://stackoverflow.com/questions/17346592/how-does-chip-8-graphics-rendered-on-screen
        private void OpCodeDXYN(Instruction instruction) {
            var xCoord = V[instruction.X] % Constants.Width;
            var yCoord = V[instruction.Y] % Constants.Height;

            var height = instruction.N;

            //Set the F register to 0 until a collision is detected
            V[0xF] = 0;

            for (int row = 0; row < height; row++) {

                var y = yCoord + row;

                //break if y is greater that max height
                if (y >= Constants.Height)
                    break;

                var x = xCoord;
                var sprite = memory[I + row];

                // Starting with most significant bit
                for (int bit = 7; bit >= 0; bit--) {

                    if (x >= Constants.Width)
                        break;

                    var currentBit = ((sprite >> bit) & 1) != 0;

                    // if current bit is true and specified pixel is on, indicate collision
                    if(currentBit && Graphics[x,y]) {
                        V[0xf] = 1;
                    } else if (currentBit) {
                        //if (Graphics[x,y]) {
                        //    Graphics[x, y] = false;
                        //} else Graphics[x,y] = true;

                        Graphics[x, y] ^= true;
                    }
                    x++;
                }
            }

            Display?.Render();
        }
    }
}
