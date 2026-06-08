namespace Nacos.V2.Remote.Responses
{
    public class ConfigFuzzyWatchNotifyResponse : CommonResponse
    {
        public override string GetRemoteType() => RemoteRequestType.Resp_Config_FuzzyWatchNotify;
    }
}
