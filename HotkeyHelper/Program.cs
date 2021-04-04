using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Configuration;
using System.Threading;

namespace HotkeyHelper
{
    partial class Program
    {
        public static bool ThreadStopSignal;

        private const VirtualKeyShort SpamKey = VirtualKeyShort.MULTIPLY;
        private const long MaxDoubleClickDelay = 200; //only do the camera moving for double-f2s within a 200ms time interval
        private const long MaxTickCount = 9000; //don't count ticks over9000 since we only require that for double-f2 recognition
        private const VirtualKeyShort RecordSeqKey = VirtualKeyShort.LCONTROL;


        static void Main()
        {
            Console.WriteLine("Initializing ClickSpam && F2 Helper...");
            // local functions boilerplate
            bool IsKeyPressed(VirtualKeyShort key) => GetAsyncKeyState((int)key) != 0;
            void ButtonDown(VirtualKeyShort key) => keybd_event((byte)key, 0, 0, UIntPtr.Zero);
            void ButtonUp(VirtualKeyShort key) => keybd_event((byte)key, 0, (uint)KEYEVENTF.KEYUP, UIntPtr.Zero);
            void ButtonPress(VirtualKeyShort key) { ButtonDown(key); ButtonUp(key); }
            Random rn = new Random();
            void ButtonPressRnd(VirtualKeyShort key)
            {
                ButtonDown(key); 
                Thread.Sleep(rn.Next(10, 50));
                ButtonUp(key);
            }
            void Chord(VirtualKeyShort firstKey, VirtualKeyShort secondKey) { ButtonDown(firstKey); ButtonDown(secondKey); ButtonUp(secondKey); ButtonUp(firstKey); }
            void MouseClick() => mouse_event((int)(MouseEventFlags.LEFTDOWN | MouseEventFlags.LEFTUP), 0, 0, 0, 0);
            // working thread
            new Thread(new ParameterizedThreadStart(delegate //not that we ever needed any params
            {
                int ticksSinceTitleUpdate = 0;
                int countWithoutSleep = 0; // make it click too much, and you're frozen
                bool currentF2IsAlreadyProcessed = false; //a button pressed stays pressed, unless specifically unpressed by the user, so make sure we only f2 once...
                long ticksWithoutF2 = 0; // if it's double-f2, we double the F2 in order to not lose the user's camera movement intention 
                bool oldState = false;
                Random r = new Random();
                

                while (!ThreadStopSignal) //we stop via ^C globally, or [Enter] in console
                {
                    if (IsKeyPressed(VirtualKeyShort.SUBTRACT) && !oldState)
                    {
                        oldState = true;
                    }

                    if (!IsKeyPressed(VirtualKeyShort.SUBTRACT) && oldState)
                    {
                        var process = Process.Start(@"C:\Users\Alex\Desktop\cliptonum.exe");
                        process?.WaitForExit();
                        oldState = false;
                    }


                    var isF2Pressed = IsKeyPressed(VirtualKeyShort.F2);
                    if (!isF2Pressed) currentF2IsAlreadyProcessed = false;
                    if (isF2Pressed && !currentF2IsAlreadyProcessed)
                    {
                        if (ticksWithoutF2 < MaxDoubleClickDelay)
                        {
                            ButtonPress(VirtualKeyShort.KEY_9); //pressing 9 twice to force-select it
                            ButtonPress(VirtualKeyShort.KEY_9); 
                        }
                        Chord(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_9); //Make group 9 out of all army
                        ButtonPress(VirtualKeyShort.KEY_0); //Select group 0
                        Chord(VirtualKeyShort.MENU, VirtualKeyShort.KEY_0); //Remake group 0 and subtract it from group 9 (that is our F2)
                        ButtonPress(VirtualKeyShort.KEY_9); //Select group 9, that is our F2 except our group 0

                        currentF2IsAlreadyProcessed = true; //Don't spam this at 1KHz please, thank you
                        ticksWithoutF2 = 0; //Just processed an F2
                    }
                    ticksWithoutF2 = Math.Min(ticksWithoutF2 + 1, MaxTickCount); //Only count to MaxTickCount. Why long, you ask?
                    if (!IsKeyPressed(SpamKey))
                    {
                        ticksSinceTitleUpdate = (ticksSinceTitleUpdate + 1) % 1000;
                        if (ticksSinceTitleUpdate == 0) Console.Title = $"ClickSpam: idle for {ticksWithoutF2} ticks";
                        Thread.Sleep(1);
                        continue;
                    } //we're only going past this point if we want clicking at 8KHz. That's A LOT of clicking.
                    if ((countWithoutSleep = (countWithoutSleep + 1) & 7) == 7) Thread.Sleep(0);
                    MouseClick();
                }

                Console.WriteLine("Ended loop");

            })).Start(); //The thread gathers, and now my watch begins. It shall not end until ^C or [Enter], I shall take no breaks (except @1KHz), hold no hooks, father no WaitHandles...
            Console.WriteLine("All systems green. [ENTER] terminates the program.");
            Console.ReadLine();
            ThreadStopSignal = true; //spoiler: the thread dies.
        }
    }
}
