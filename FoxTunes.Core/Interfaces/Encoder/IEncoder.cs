using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IEncoder : IBaseComponent
    {
        IEncoderOutputPath GetOutputPath();

        IEncoderOutputPath GetOutputPath(string path = null);

        IReportComponent GetReport(EncoderItem[] encoderItems);

        Task<EncoderItem[]> Encode(IFileData[] fileDatas, string profile, bool report);

        Task<EncoderItem[]> Encode(IFileData[] fileDatas, IEncoderOutputPath outputPath, string profile, bool report);
    }
}
