using System.Threading.Tasks;

namespace JTCommonTest.Interfaces
{
    public interface IBluetooth
    {
        Task Scan(int scanDuration, string serviceUuid = "");
    }
}
