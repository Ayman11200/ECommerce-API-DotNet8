using Ecom.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.infrastructure.Repositires.Service
{
    public class ImageManagementService : IImageManagementService
    {
        private readonly IFileProvider fileProvider;
        public ImageManagementService(IFileProvider fileProvider)
        {
         this.fileProvider = fileProvider;   
        }

        public async Task<List<string>> AddImageAsync(IFormFileCollection files, string ProductName)
        {
            var urls = new List<string>();

           var folderPath = Path.Combine(
                "wwwroot",
                "Images",
                ProductName
                );

            Directory.CreateDirectory(folderPath);

            foreach (var image in files)
            {
                if(image.Length > 0)
                {
                    var ImageName = image.FileName;

                    var physicalPath = Path.Combine(folderPath, ImageName);

                    using var stream =
                    new FileStream(
                        physicalPath,
                        FileMode.Create);

                    await image.CopyToAsync(stream);

                    urls.Add($"/Images/{ProductName}/{ImageName}");
                }

            }

            return urls;

        }

        public void DeleteImage(string ImageName)
        {
            var image = fileProvider.GetFileInfo(ImageName);

            var PhysicalPath = image.PhysicalPath;

            File.Delete(PhysicalPath);

        }
    }
}
