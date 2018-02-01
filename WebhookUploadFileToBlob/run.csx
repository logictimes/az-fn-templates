#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

/*

Request Body:
{
"Author": "username",
"FileName": "YOURFILE.TXT
"ContentType": "text/plain"
"StringBase64": "VGhpcyBpcyBzYW1wbGUgdGV4dCB1cGxvYWRlZCB1c2luZyBVcGxvYWRGaWxlVG9CbG9iIEZ1bmN0aW9uDQoNCkp1c3QgVGVzdA=="
}
*/


[FunctionName("UploadFileToBlob")]
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
            string accessKey = "";
            string accountName = "";
            string containerName = "";
            string connectionString;
            CloudStorageAccount storageAccount;
            CloudBlobClient client;
            CloudBlobContainer container;
            CloudBlockBlob blob;

            HttpStatusCode returnCode;
            string returnBody;
            try
            {
				dynamic body = await req.Content.ReadAsStringAsync();
				var input_data = JsonConvert.DeserializeObject<UploadedFile>(body as string);


                //Build Azure Storage Connection String
                accountName = System.Environment.GetEnvironmentVariable("StorageAccountName");
                accessKey = System.Environment.GetEnvironmentVariable("AccessKey");
                containerName = System.Environment.GetEnvironmentVariable("storageContainer");
                connectionString = "DefaultEndpointsProtocol=https;AccountName=" + accountName + ";AccountKey=" + accessKey + ";EndpointSuffix=core.windows.net";
                
				
				//validate connectionstring
				storageAccount = CloudStorageAccount.Parse(connectionString);

                
				//create client
				client = storageAccount.CreateCloudBlobClient();

                //getting Container
				container = client.GetContainerReference(containerName);

                //Start Upload session
                blob = container.GetBlockBlobReference(input_data.FileName);

                //if file exists - create snapshot
                if (blob.Exists()) { 
                    blob.CreateSnapshot();
                }

                //adding meta 
				blob.Properties.ContentType = input_data.ContentType;
                blob.Metadata["Author"] = input_data.Author;
                
                //Decode Base64 String to Byte Array
                byte[] fileBytes = Convert.FromBase64String(input_data.DataBase64);
				
				//upload body
				await blob.UploadFromByteArrayAsync(fileBytes,0, fileBytes.Length);

                //return success and URI
                returnCode = HttpStatusCode.OK;
                returnBody = blob.StorageUri.PrimaryUri.AbsoluteUri;
            }
            catch (Exception e)
            {
                // Exception 
                log.Error("Error occured during the opertion: " + e.ToString());
                returnCode = HttpStatusCode.BadRequest;
                returnBody = $"ERROR! " + e;
            }


            return req.CreateResponse(returnCode, returnBody);




}

    public class UploadedFile
    {
        public string Author { get; set; }
        public string FileName { get; set; }
		public string ContentType { get; set; }
        public string StringBase64 { get; set; }
    }
