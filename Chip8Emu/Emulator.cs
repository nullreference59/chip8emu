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
            switch (inst) {
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
                case 0x2000:
                    //Subroutine
                    OpCode2NNN(instruction);
                    break;
                case 0x3000:
                    //skip instruction if VX == NN
                    OpCode3NNN(instruction);
                    break;
                case 0x4000:
                    //skip instruction if VX != NN 
                    OpCode4NNN(instruction);
                    break;
                case 0x5000:
                    // skip instruction if VX==VY
                    OpCode5XY0(instruction);
                    break;
                case 0x6000:
                    //Set V[X] to NN
                    OpCode6XNN(instruction);
                    break;
                case 0x7000:
                    //Add NN to V[X]
                    OpCode7XNN(instruction);
                    break;
                case 0x8000:
                    switch (instruction.OpCode & 0x000F) {
                        case 0:
                            //VX is set to VY
                            OpCode8XY0(instruction);
                            break;
                        case 1:
                            // VX = VX | VY
                            OpCode8XY1(instruction);
                            break;
                        case 2:
                            // VX = VX & VY
                            OpCode8XY2(instruction);
                            break;
                        case 3:
                            // VX = VX XOR VY
                            OpCode8XY3(instruction);
                            break;
                        case 4:
                            // VX = VX + VY
                            OpCode8XY4(instruction);
                            break;
                        case 5:
                            // VX = VX - VY
                            OpCode8XY5(instruction);
                            break;
                        case 6:
                            // Shift the value of VX on bit right
                            OpCode8XY6(instruction);
                            break;
                        case 7:
                            // VX = VY - VX
                            OpCode8XY7(instruction);
                            break;
                        case 0xE:
                            // Shift the value of VX on bit left
                            OpCode8XYE(instruction);
                            break;
                    }
                    break;
                case 0x9000:
                    //Skip instruction if VX!=VY
                    OpCode9XY0(instruction);
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

        private void OpCode2NNN(Instruction instruction) {
            stack.Push(PC);
            PC = instruction.NNN;
        }

        private void OpCode3NNN(Instruction instruction) {
            if (V[instruction.X] == instruction.NN) {
                PC += 2;
            }
        }

        private void OpCode4NNN(Instruction instruction) {
            if (V[instruction.X] != instruction.NN) {
                PC += 2;
            }
        }

        private void OpCode5XY0(Instruction instruction) {
            if (V[instruction.X] == V[instruction.Y]){
                PC += 2;
            }
        }

        private void OpCode6XNN(Instruction instruction) {
            V[instruction.X] = instruction.NN;
        }

        private void OpCode7XNN(Instruction instruction) {
            V[instruction.X] += instruction.NN;
        }

        private void OpCode8XY0(Instruction instruction) {
            V[instruction.X] = V[instruction.Y];
        }

        private void OpCode8XY1(Instruction instruction) {
            V[instruction.X] = (byte) (V[instruction.X] | V[instruction.Y]);
        }

        private void OpCode8XY2(Instruction instruction) {
            V[instruction.X] = (byte)(V[instruction.X] & V[instruction.Y]);
        }
        private void OpCode8XY3(Instruction instruction) {
            V[instruction.X] = (byte)(V[instruction.X] ^ V[instruction.Y]);
        }
        private void OpCode8XY4(Instruction instruction) {
            if (V[instruction.Y] > (0xFF - V[instruction.X]))
                V[0xF] = 1;
            else
                V[0xF] = 0;

            V[instruction.X] = (byte)(V[instruction.X] + V[instruction.Y]);
        }
        private void OpCode8XY5(Instruction instruction) {
            if (V[instruction.Y] > V[instruction.X])
                V[0xF] = 0;
            else
                V[0xF] = 1;

            V[instruction.X] = (byte)(V[instruction.X] - V[instruction.Y]);
        }
        private void OpCode8XY6(Instruction instruction) {
            V[0xF] = (byte)(V[instruction.X] & 0x1);
            V[instruction.X] >>= 0x1;
        }

        private void OpCode8XY7(Instruction instruction) {
            int diff = V[instruction.Y] - V[instruction.X];
            V[instruction.X] = (byte)(diff & 0xFF);
            V[0xF] = (byte)(diff > 0 ? 1 : 0);

            V[instruction.X] = (byte)(V[instruction.Y] - V[instruction.X]);
        }

        private void OpCode8XYE(Instruction instruction) {
            V[0xF] = (byte)((V[instruction.X] & 0x80) >> 7);
            V[instruction.X] <<= 0x1;
        }

        private void OpCode9XY0(Instruction instruction) {
            if(V[instruction.X] != V[instruction.Y]) {
                PC += 2;
            }
        }

        private void OpCodeANNN(Instruction instruction) {
            I = instruction.NNN;
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
                       
                        // XOR current bit and current pixel
                        Graphics[x, y] ^= true;
                    }
                    x++;
                }
            }

            Display?.Render();
        }
    }
}
