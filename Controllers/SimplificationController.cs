using AssinaAiApi.Entities;
using AssinaAiApi.Models;
using AssinaAiApi.Repository;
using Azure;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Buffers.Text;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Xml.Linq;
using Tesseract;

namespace AssinaAiApi.Controllers
{
    public static class SimplificationController
    {
        private static WebApplication currentContext;

        [EndpointDescription("EndPoins da Simplificação")]
        public static void ConfigureSimplificationApi(this WebApplication app)
        {
            currentContext = app;

            app.MapPost("Add/Simplification", AddSimplification)
            .WithName("AddSimplification")
            .WithDescription("Upload do documento - Deve ser enviado um form-data com o arquivo via Post")
            .WithOpenApi();

            app.MapGet("/Simplification", GetSimplification)
            .WithName("GetSimplification")
            .WithDescription("Consultar simplificação")
            .WithOpenApi();

            app.MapGet("/Simplification/{id}", GetSimplificationById)
            .WithName("GetSimplificationById")
            .WithDescription("Consultar simplificação pelo Id")
            .WithOpenApi();

            app.MapDelete("/Simplification/{id}", DeleteSimplification)
            .WithName("DeleteSimplification")
            .WithDescription("Deletar simplificação")
            .WithOpenApi();
        }

        private static string ConvertIFormFileToBase64(IFormFile file)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Copy the IFormFile content to the memory stream
                file.CopyTo(memoryStream);

                // Convert the memory stream to a byte array
                byte[] fileBytes = memoryStream.ToArray();

                // Convert the byte array to base64
                string base64String = Convert.ToBase64String(fileBytes);

                return base64String;
            }
        }

        private static string GetTextFromImg(string filePath)
        {
            TesseractEngine engine = new TesseractEngine("./OCR/Img/tessdata", "por", EngineMode.Default);
            var _filePath = Directory.GetCurrentDirectory() + "\\" + filePath.Replace("/", "\\");

            var img = Pix.LoadFromFile(_filePath);
            Page page = engine.Process(img);

            string result = page.GetText();

            return result;
        }

        private static string GetTextFromDocx(string filePath)
        {
            string result = "";
            var _filePath = Directory.GetCurrentDirectory() + "\\" + filePath.Replace("/", "\\");

            using (WordprocessingDocument doc = WordprocessingDocument.Open(_filePath, false))
            {
                var body = doc.MainDocumentPart.Document.Body;

                result = body.InnerText;
            }

            return result;
        }

        public static string ExtractTextFromPdf(string pdfFilePath)
        {
            var _filePath = Directory.GetCurrentDirectory() + "\\" + pdfFilePath.Replace("/", "\\");

            PdfReader reader = new PdfReader(_filePath);
            StringBuilder text = new StringBuilder();

            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                text.Append(PdfTextExtractor.GetTextFromPage(reader, page));
            }

            reader.Close();
            return text.ToString();
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> AddSimplification(DataContext context, HttpRequest request)
        {
            try
            {
                var file = request.Form.Files.FirstOrDefault();
                var fileExtension = "";

                if (file == null)
                    return Results.Problem("Deve ser informado um arquivo");
                else
                {
                    fileExtension = System.IO.Path.GetExtension(file.FileName);
                    if (fileExtension != ".doc" && fileExtension != ".docx" && fileExtension != ".pdf" && fileExtension != ".png" && fileExtension != ".jpg")
                        return Results.Problem("Formato de arquivo inválido, formado aceitos (docx/pdf/png/jpg)");
                }

                var token = request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

                var tokenHandler = new JwtSecurityTokenHandler();

                var key = Encoding.ASCII.GetBytes(currentContext.Configuration["Jwt:Key"]);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.FirstOrDefault().Value);

                var PersonInsert = await context.Person.FindAsync(userId);

                if (PersonInsert == null || (PersonInsert != null && PersonInsert.Id == 0))
                    return Results.Problem("Usuário não identificado");

                var folder = $"uploads/{DateTime.Now.ToString("yyyy")}";
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                var filePath = $"{folder}/{DateTime.Now.ToString("MM-dd-hh-mm-ss")}-{file.FileName}";
                using var steam = File.OpenWrite(filePath);
                await file.CopyToAsync(steam);
                steam.Close();

                var archive = new Archive()
                {
                    Extension = fileExtension,
                    FileName = file.FileName,
                    Path = filePath,
                    Base64 = ConvertIFormFileToBase64(file),
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    UniqueId = Guid.NewGuid(),
                };

                await context.Archive.AddAsync(archive);

                var textFile = "";

                if (fileExtension == ".png" || fileExtension == ".jpg")
                {
                    textFile = GetTextFromImg(filePath);
                }
                else if (fileExtension == ".docx" || fileExtension == ".doc")
                {
                    textFile = GetTextFromDocx(filePath);
                }
                else if (fileExtension == ".pdf")
                {
                    textFile = ExtractTextFromPdf(filePath);
                }

                var simplification = new Simplification()
                {
                    PersonId = PersonInsert.Id,
                    Name = $"Simplificação arquivo {file.FileName}",
                    OriginalTextFile = textFile,
                    Resume = "",
                    CreateDate = DateTime.Now,
                    UpdateDate = DateTime.Now,
                    UniqueId = Guid.NewGuid()
                };

                await context.Simplification.AddAsync(simplification);

                //GetChatGptResponse

                await context.SaveChangesAsync();

                return Results.Ok(simplification);
            }
            catch (Exception ex)
            {
                return Results.Problem("Erro ao enviar arquivo");
            }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> GetSimplification(DataContext context, HttpRequest request)
        {
            var token = request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            var tokenHandler = new JwtSecurityTokenHandler();

            var key = Encoding.ASCII.GetBytes(currentContext.Configuration["Jwt:Key"]);
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(jwtToken.Claims.FirstOrDefault().Value);

            var PersonInsert = await context.Person.FindAsync(userId);

            if (PersonInsert == null || (PersonInsert != null && PersonInsert.Id == 0))
                return Results.Problem("Usuário não identificado");

            return Results.Ok(await context.Simplification.Where(x => x.PersonId == PersonInsert.Id).ToListAsync());
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> GetSimplificationById(DataContext context, int id) 
        { 
            var result = await context.Simplification.FindAsync(id);

            return Results.Ok(result);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        private static async Task<IResult> DeleteSimplification(DataContext context, int id)
        {
            var result = await context.Simplification.FindAsync(id);

            if(result != null && result.Id > 0)
            {
                context.Remove(result);
                await context.SaveChangesAsync();
            }

            return Results.Ok(result);
        }
    }
}
