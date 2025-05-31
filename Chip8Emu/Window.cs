using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8Emu {
    public interface IDisplay {
        void Render();
    }

    public class Window :GameWindow, IDisplay {

        public IEmulator? Emulator;
        public Window(int height, int width, string title) : base(GameWindowSettings.Default, 
            new NativeWindowSettings() { ClientSize=new Vector2i(width,height), Title = title }) {

        }

        protected override void OnLoad() {
            base.OnLoad();

            GL.ClearColor(Color.Black);
            GL.Color3(Color.White);
            GL.Ortho(0, Constants.Width, Constants.Height, 0, -1, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            SwapBuffers();
        }

        public override void Run() {

            Emulator?.Run();
            base.Run();
        }

        public void Render() {
            var buffer = Emulator?.Graphics;

            if(buffer == null) {
                return;
            }

            GL.Clear(ClearBufferMask.ColorBufferBit);

            for( int y = 0; y < Constants.Height; y++) {
                for(int x = 0; x < Constants.Width; x++) {
                    if (buffer[x, y]) {
                        GL.Rect(x, y, x + 1, y + 1);
                    }
                }
            }

            SwapBuffers();
        }
    }
}
