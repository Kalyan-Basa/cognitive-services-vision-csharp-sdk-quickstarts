using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ExtractText
{
    class Program
    {
        // subscriptionKey = "0123456789abcdef0123456789ABCDEF"
        private const string subscriptionKey = "4f44ee46b2d7408dbc8f694f687fee41";

        // localImagePath = @"C:\Documents\LocalImage.jpg"
        private const string localImagePath = @"C:/Users/kbasa/Downloads/8001.jpg";

        private const string remoteImageUrl =
            "https://upload.wikimedia.org/wikipedia/commons/thumb/d/dd/Cursive_Writing_on_Notebook_paper.jpg/800px-Cursive_Writing_on_Notebook_paper.jpg";

        private const int numberOfCharsInOperationId = 36;


        static void Main(string[] args)
        {
            ComputerVisionClient computerVision = new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(subscriptionKey),
                new System.Net.Http.DelegatingHandler[] { });

            // You must use the same region as you used to get your subscription
            // keys. For example, if you got your subscription keys from westus,
            // replace "westcentralus" with "westus".
            //
            // Free trial subscription keys are generated in the westcentralus
            // region. If you use a free trial subscription key, you shouldn't
            // need to change the region.

            // Specify the Azure region
            computerVision.Endpoint = "https://westcentralus.api.cognitive.microsoft.com";

            Console.WriteLine("Images being analyzed ...");

            //var t1 = ExtractRemoteTextAsync(computerVision, remoteImageUrl);
            var t2 = Task.FromResult<string>(ExtractLocalTextAsync(computerVision, localImagePath).Result);

            Console.WriteLine(t2.Result);
            //Task.WhenAll(t2).Wait(5000);
            //Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }



        // Read text from a remote image
        private static async Task ExtractRemoteTextAsync(
            ComputerVisionClient computerVision, string imageUrl)
        {
            if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
            {
                Console.WriteLine(
                    "\nInvalid remoteImageUrl:\n{0} \n", imageUrl);
                return;
            }

            // Start the async process to read the text
            BatchReadFileHeaders textHeaders =
                await computerVision.BatchReadFileAsync(
                    imageUrl);

            await GetTextAsync(computerVision, textHeaders.OperationLocation);
        }

        // Recognize text from a local image
        private static async Task<string> ExtractLocalTextAsync(
            ComputerVisionClient computerVision, string imagePath)
        {
            string result = string.Empty;
            if (!File.Exists(imagePath))
            {
                Console.WriteLine(
                    "\nUnable to open or read localImagePath:\n{0} \n", imagePath);
                return result;
            }

            using (Stream imageStream = File.OpenRead(imagePath))
            {
                // Start the async process to recognize the text
                BatchReadFileInStreamHeaders textHeaders =
                    await computerVision.BatchReadFileInStreamAsync(
                        imageStream);

                result = await Task.FromResult<string>(GetTextAsync(computerVision, textHeaders.OperationLocation).Result);
            }

            return result;
        }

        // Retrieve the recognized text
        private static async Task<string> GetTextAsync(
            ComputerVisionClient computerVision, string operationLocation)
        {
            // Retrieve the URI where the recognized text will be
            // stored from the Operation-Location header
            string operationId = operationLocation.Substring(
                operationLocation.Length - numberOfCharsInOperationId);

            Console.WriteLine("\nCalling GetHandwritingRecognitionOperationResultAsync()");
            ReadOperationResult result =
                await computerVision.GetReadOperationResultAsync(operationId);

            // Wait for the operation to complete
            int i = 0;
            int maxRetries = 10;
            while ((result.Status == TextOperationStatusCodes.Running ||
                    result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                Console.WriteLine(
                    "Server status: {0}, waiting {1} seconds...", result.Status, i);
                await Task.Delay(1000);

                result = await computerVision.GetReadOperationResultAsync(operationId);
            }

            // Display the results
            Console.WriteLine();
            var recResults = result.RecognitionResults;
            StringBuilder sb = new StringBuilder();
            foreach (TextRecognitionResult recResult in recResults)
            {
                foreach (Line line in recResult.Lines)
                {
                    // Console.WriteLine(line.Text);
                    sb.Append(line.Text);
                }
            }

            Console.WriteLine();

            return sb.ToString();
        }
    }
}