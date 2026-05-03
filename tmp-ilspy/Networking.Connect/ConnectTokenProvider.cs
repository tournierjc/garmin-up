using Networking.Auth;
using RestSharp.Authenticators;

namespace Networking.Connect;

internal static class ConnectTokenProvider
{
	private const string ConsumerKey = "ea51b5fb-a515-4e74-9077-0d91f4f9c855";

	private const string ConsumerSecret = "Kgh7H6oRTpk8vITrYcn2OnBQvESbeaWGPtv";

	public static OAuth1Authenticator GetAuthenticatorForRequestToken()
	{
		return OAuth1Authenticator.ForRequestToken("ea51b5fb-a515-4e74-9077-0d91f4f9c855", "Kgh7H6oRTpk8vITrYcn2OnBQvESbeaWGPtv");
	}

	public static OAuth1Authenticator GetAuthenticatorForAccessToken(ConnectAuth auth)
	{
		return OAuth1Authenticator.ForAccessToken("ea51b5fb-a515-4e74-9077-0d91f4f9c855", "Kgh7H6oRTpk8vITrYcn2OnBQvESbeaWGPtv", auth.Token, auth.TokenSecret);
	}
}
