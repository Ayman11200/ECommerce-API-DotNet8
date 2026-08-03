using Ecom.Core.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecom.Core.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailDto emailDTO);
    }
}
