﻿namespace Testing.DependencyInjection;

public class MessageWriter : IMessageWriter
{
    public void Write(string message)
    {
        Console.WriteLine($"[{DateTime.Now:G}] {message}");
    }
}
