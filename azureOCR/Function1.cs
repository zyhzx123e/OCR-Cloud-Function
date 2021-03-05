using System;
using System.Drawing;
using System.Drawing.Imaging;

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tesseract;
using System.Reflection;
using Spire.Pdf;
using System.Net;
using System.Net.Http;

namespace azureOCR
{
    public static class Function1
    {
       
        [FunctionName("processOCR")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            string requestBody = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string blob_url = data["blob_url"];
            Console.WriteLine("processOCR blob_url:" + blob_url);

            PdfDocument doc = new PdfDocument();
            if (blob_url!=null && blob_url!="")
            { 
                var client = new HttpClient();
                var response = await client.GetAsync(blob_url);

                using (var stream = await response.Content.ReadAsStreamAsync())
                {  
                    doc.LoadFromStream(stream); 
                }
               
            }
            else
            {
                var obj = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    status = 406,
                    message=  "Missing blob_url"
                });
                return (ActionResult)new BadRequestObjectResult(obj);
                 
            }

          

            //var file_path = "./PO 0100180713- EMAIL.PDF"; 
            //byte[] bytes_file = System.IO.File.ReadAllBytes(file_path);
             
            
           // doc.LoadFromBytes(bytes_file);

            Image bmp = Bitmap.FromStream(doc.SaveAsImage(0));

            Image emf = Bitmap.FromStream(doc.SaveAsImage(0, Spire.Pdf.Graphics.PdfImageType.Bitmap));

            Image zoomImg = new Bitmap((int)(emf.Size.Width * 2), (int)(emf.Size.Height * 2));

            
            using (Graphics g = Graphics.FromImage(zoomImg))

            {

                g.ScaleTransform(2.0f, 2.0f);

                g.DrawImage(emf, new Rectangle(new Point(0, 0), emf.Size), new Rectangle(new Point(0, 0), emf.Size), GraphicsUnit.Pixel);

            }

            bmp.Save("convertToBmp.bmp", System.Drawing.Imaging.ImageFormat.Bmp);

            //System.Diagnostics.Process.Start("convertToBmp.bmp");

            emf.Save("convertToEmf.png", System.Drawing.Imaging.ImageFormat.Png);

            //          System.Diagnostics.Process.Start("convertToEmf.png");
            Console.Write("zoomImg zoomImg done");
            zoomImg.Save("convertToZoom.png", System.Drawing.Imaging.ImageFormat.Png);

            //            System.Diagnostics.Process.Start("convertToZoom.png");
            byte[] bytes_img = (byte[])(new ImageConverter()).ConvertTo(zoomImg, typeof(byte[]));
             

            var str_res = "";
            var file = "./res.html";
            using (var engine = new Tesseract.TesseractEngine("./tessdata", "eng", EngineMode.Default))
            {
                // OCR entire document
                using (var img = Pix.LoadFromMemory(bytes_img))
                {
                    using (var page = engine.Process(img,"image",PageSegMode.Auto))
                    {
                        // OCR entire document
                        var path=Path.GetDirectoryName(file)+"\\"+Path.GetFileNameWithoutExtension(file);
                        using (var renderer = ResultRenderer.CreatePdfRenderer(path, @"./tessdata",false))
                        {
                            using ( renderer.BeginDocument(Path.GetFileNameWithoutExtension(file)))
                            {
                                renderer.AddPage(page);
                                str_res = page.GetHOCRText(0, true);
                              // var boxtext = page.GetBoxText(0);
                                Console.Write("ocr str_res:" + str_res);

                               // var LSTMtext = page.GetLSTMBoxText(0);
                                //Console.Write("ocr LSTMtext:" + LSTMtext);


                            }
                        }
                         
                    }


                }


                 
            } 

             
           
           /* var obj = Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                status = 406,
                message = str_res
            });*/
            return (ActionResult)new OkObjectResult(str_res);


        }
    }
}
