namespace FocusSession.Configuration
{
    public class SlackConfig
    {       
        public static string InitializeCacheJSONFile { get; } = "{\"botAuthToken\":\"bottoken-bottoken-bottoken-bottoken\",\"memberId\":\"yourMemberId\",\"memberName\":\"yourSlackName\"}";

        public string botAuthToken { get; set; }
        public string memberId { get; set; }
        public string memberName { get; set; }

        // these are original values, that could all be used together with the slack API for c# by Inumedia taken from https://github.com/Inumedia/SlackAPI, see SlackAPI Folder
        //public static string InitializeCacheJSONFile { get; } = "{\"userAuthToken\":\"token - tokentoken - tokentoken - tokentoken - token\",\"botAuthToken\":\"bottoken-bottoken-bottoken-bottoken\",\"memberId\":\"yourMemberId\",\"memberName\":\"yourSlackName\",\"testChannel\":\"SuperSecretChannel\",\"directMessageUser\":\"someUserId\",\"clientId\":\"your-special-id\",\"clientSecret\":\"such-special-secret-key\",\"redirectUrl\":\"http://redirecturl\",\"authUsername\":\"user@domain.com\",\"authPassword\":\"userpassword\",\"authWorkspace\":\"workspace\"}";
        //public string userAuthToken { get; set; }
        //public string botAuthToken { get; set; }
        //public string memberId { get; set; }
        //public string memberName { get; set; }
        //public string testChannel { get; set; }
        //public string directMessageUser { get; set; }
        //public string clientId { get; set; }
        //public string clientSecret { get; set; }
        //public string redirectUrl { get; set; }
        //public string authUsername { get; set; }
        //public string authPassword { get; set; }
        //public string authWorkspace { get; set; }

    }
}