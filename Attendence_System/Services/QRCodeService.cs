using System;
using QRCoder;
using iTextSharp.text.pdf.qrcode;

namespace Attendence_System.Services
{
    public class QRCodeService : IQRCodeService
    {
        public string GenerateQRCode(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new BitmapByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

        public string GenerateBarcode(string data)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            try
            {
                var barcode = new NetBarcode.Barcode(data, NetBarcode.Type.Code128B, false);
                return $"data:image/png;base64,{barcode.GetBase64Image()}";
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
