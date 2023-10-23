using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Tesseract;

namespace GetCustomerNameFromPrinter_sPlanWindow
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: ConsoleApp.exe" + args[2] + args[1] + args[0]);
                Console.ReadKey();
                return;
            }
            string referenceNum = args[2];
            string amount = args[1];
            string customerName = GetTextFromScreen(args[0]);

            var check = new Check
            {
                ReferenceNum = double.Parse(referenceNum),
                Amount = decimal.Parse(amount),
                CustomerName = customerName
            };

            await PostCheckAsync(check);
        }

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public static string GetTextFromScreen(string args)
        {
            System.Drawing.Rectangle bounds;

            IntPtr hWnd = (IntPtr)Convert.ToInt32(args);
            RECT rect;
            GetWindowRect(hWnd, out rect);
            bounds = new System.Drawing.Rectangle(rect.Left + 129, rect.Top + 35, 900, 30);

            byte[] byteFile;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), System.Drawing.Point.Empty, bounds.Size);
                }
                ImageConverter converter = new ImageConverter();
                byteFile = (byte[])converter.ConvertTo(bitmap, typeof(byte[]));

            }

            string dataPath = @"C:\Users\Jeff\source\repos\GetCustomerNameFromPrinter'sPlanWindow\tessdata\";

            using (var engine = new TesseractEngine(dataPath, "eng", EngineMode.Default))
            {

                using (var img = Pix.LoadFromMemory(byteFile))
                {
                    using (var page = engine.Process(img))
                    {
                        string text = page.GetText();
                        string delimitedStr = DelimitString(text);
                        return delimitedStr;
                    }
                }
            }
        }

        public static string DelimitString(string str)
        {
            int index = str.IndexOf(" (");
            string firstPart = str.Substring(0, index);
            return firstPart;
        }

        static async Task PostCheckAsync(Check check)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("http://localhost:6428/api/Checks/"); // Replace with your API's URL
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                var response = await httpClient.PostAsJsonAsync("", check);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Check created successfully!");
                }
                else
                {
                    Console.WriteLine($"Failed to create the check. Status code: {response.StatusCode}");
                    Console.ReadKey();
                    // Handle error cases here
                }
            }
        }
    }
    public class Check
    {
        public double ReferenceNum { get; set; }
        public decimal Amount { get; set; }
        public string CustomerName { get; set; }
    }
}