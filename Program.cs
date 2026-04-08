//HashClient
using System;
using System.Net.Sockets;
using System.Text;
using System.IO;

class Program
{
    static TcpClient client;
    static NetworkStream stream;
    static StreamReader reader;

    static void Main(string[] args)
    {
        try
        {
            client = new TcpClient("localhost", 9000);
            stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.ASCII);

            Console.WriteLine();
            Console.WriteLine("Connected to server!");

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string[] parts = input.Split(' ');
                string command = parts[0].ToLower();

                if (command == "list")
                {
                    Send("LIST\n");
                    ReadResponse();
                }
                else if (command == "get")
                {
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: get <hash>");
                        continue;
                    }

                    HandleGet(parts[1]);
                }
                else if (command == "delete")
                {
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Usage: delete <hash>");
                        continue;
                    }

                    HandleDelete(parts[1]);
                }
                else if (command == "upload")
                {
                    if (parts.Length < 3)
                    {
                        Console.WriteLine("Usage: upload <file> <description>");
                        continue;
                    }

                    HandleUpload(parts[1], parts[2]);
                }
                else if (command == "exit")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Unknown command");
                }
            }

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static void Send(string text)
    {
        byte[] data = Encoding.ASCII.GetBytes(text);
        stream.Write(data, 0, data.Length);
    }

    static void ReadResponse()
    {
        string response = reader.ReadLine();
        Console.WriteLine("Server: " + response);
    }

    static void HandleGet(string hash)
    {
        Send($"GET {hash}\n");

        string response = reader.ReadLine();
        Console.WriteLine("Server: " + response);

        if (!response.StartsWith("200"))
        {
            return;
        }

        string[] parts = response.Split(' ');
        int length = int.Parse(parts[2]);
        string description = parts[3];

        byte[] buffer = new byte[length];
        int totalRead = 0;

        while (totalRead < length)
        {
            int read = stream.Read(buffer, totalRead, length - totalRead);

            if (read == 0)
            {
                Console.WriteLine("Connection lost!");
                break;
            }

            totalRead += read;
        }

        string fileName = "down_" + description;
        File.WriteAllBytes(fileName, buffer);

        Console.WriteLine($"Saved as {fileName}");
    }

    static void HandleUpload(string filePath, string description)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }

        byte[] data = File.ReadAllBytes(filePath);

        string header = $"UPLOAD {data.Length} {description}\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(header);

        // DEBUG (dôležité!)
        Console.WriteLine($"Sending: {header}");

        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(data, 0, data.Length);

        string response = reader.ReadLine();
        Console.WriteLine("Server: " + response);
    }

    static void HandleDelete(string hash)
    {
        Send($"DELETE {hash}\n");

        string response = reader.ReadLine();
        Console.WriteLine("Server: " + response);
    }
}