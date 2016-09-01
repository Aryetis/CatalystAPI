﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Catalyst;
using Catalyst.Memory;
using Catalyst.Display;
using Catalyst.Input;

namespace LibraryTestingProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            InputController.EnableInputHook();
            InputController.MakeProcessSpecific("MirrorsEdgeCatalyst");

            Overlay.Enable(true);
            Overlay.AddManualField("noclip", "NOCLIP OFF");

            var mem = new MemoryManager();
            mem.OpenProcess("MirrorsEdgeCatalyst");

            var pinfo = new PlayerInfo(mem);
            var ginfo = new GameInfo(mem);

            bool noclip = false;
            DIKCode hotkey = DIKCode.F1;

            DIKCode speedup = DIKCode.LEFT;
            DIKCode speeddown = DIKCode.RIGHT;

            bool lpress = false;
            bool cpress = false;

            Vec3 pos = Vec3.Zero;
            Vec3 dpos;
            Vec3 dir = Vec3.Zero;

            float speed = 0.5f;
            float loopspeed;

            Overlay.AddAutoField("speed", () => speed, "SPEED: {0}");

            while (true)
            {
                lpress = cpress;
                cpress = InputController.IsKeyPressed(hotkey);

                if (InputController.IsKeyPressed(speedup))
                    speed += 0.05f;

                if (InputController.IsKeyPressed(speeddown))
                    speed -= 0.05f;

                if (lpress == false && cpress == true)
                {
                    noclip = !noclip;
                    Overlay.UpdateManualField("noclip", "NOCLIP " + (noclip ? "ON" : "OFF"));

                    if (noclip)
                    {
                        pos = pinfo.GetPosition();
                        //ginfo.SetTimescale(0);
                    }
                    else
                    {
                        //ginfo.SetTimescale(1);
                    }
                }

                if (noclip)
                {
                    dir = pinfo.GetCameraYawVector();
                    dpos = Vec3.Zero;

                    loopspeed = speed * 0.01f;

                    if (InputController.IsGameActionPressed(GameAction.MoveForward))
                        dpos += dir * loopspeed;

                    if (InputController.IsGameActionPressed(GameAction.MoveRight))
                        dpos += dir.Right * loopspeed;

                    if (InputController.IsGameActionPressed(GameAction.MoveLeft))
                        dpos += dir.Left * loopspeed;

                    if (InputController.IsGameActionPressed(GameAction.MoveBackward))
                    {
                        dpos -= dir * loopspeed;
                    }

                    if (InputController.IsKeyPressed(DIKCode.SPACE))
                    {
                        dpos += Vec3.AxisY * loopspeed;
                    }

                    if (InputController.IsKeyPressed(DIKCode.LSHIFT))
                    {
                        dpos -= Vec3.AxisY * loopspeed;
                    }

                    pos += dpos;
                    pinfo.SetPosition(pos);
                }

                Thread.Sleep(10);
            }
        }
    }
}
