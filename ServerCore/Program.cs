﻿using System;

namespace ServerCore
{
    internal class Program
    {
        static void MainThread()
        {
            Console.WriteLine("Hello Thread!");
        }
        static void Main(string[] args)
        {
            Thread t = new Thread(MainThread);
            t.IsBackground = true;
            t.Start();

            Console.WriteLine("Hello, World!");
        }
    }
}