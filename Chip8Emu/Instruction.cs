using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu {
    public class Instruction {
        public ushort OpCode { get; set; }
        public ushort NNN { get; set; }
        public byte NN { get; set; }
        public byte N { get; set; }
        public byte X { get; set; }
        public byte Y { get; set; }
    }
}
