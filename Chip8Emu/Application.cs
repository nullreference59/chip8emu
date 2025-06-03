using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu {
    public class Application {
        public static void Main() {

            // Get the ibm test rom
            var testRomPath = "J:\\dev\\chip8emu\\roms\\ibm-logo.ch8";

            var rom = File.ReadAllBytes(testRomPath);



            var display = new Window(512, 1024, "Chip 8 Emulator");
            var emulator = new Emulator();

            emulator.Display = display;
            display.Emulator = emulator;

            emulator.LoadRom(rom);
            emulator.Reset();
            display.Run();
        }
    }
}
