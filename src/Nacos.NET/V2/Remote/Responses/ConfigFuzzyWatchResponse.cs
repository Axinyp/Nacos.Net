namespace Nacos.V2.Remote.Responses
{
    public class ConfigFuzzyWatchResponse : CommonResponse
    {
        public override string GetRemoteType() => RemoteRequestType.Resp_Config_FuzzyWatch;
    }
}
