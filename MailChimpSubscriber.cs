#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define ENABLE_MAILCHIMP_ERROR_LOG
#define ENABLE_MAILCHIMP_SUCCESS_LOG
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Text;
using System.Net.Mail;
using JetBrains.Annotations;
using UnityEngine.Networking;


namespace Fiftytwo
{
    [PublicAPI]
    public class MailChimpSubscriber : MonoBehaviour
    {
        private const string UrlFormat = "https://{0}.api.mailchimp.com/3.0/lists/{1}/members";
        private const string JsonFormat = "{{\"email_address\":\"{0}\", \"status\":\"subscribed\"}}";

        public MailChimpEvent SubscribeSuccess = new MailChimpEvent();
        public MailChimpEvent SubscribeError = new MailChimpEvent();

        [SerializeField] private string _apiKey = string.Empty;
        [SerializeField] private string _listId = string.Empty;


        public void Subscribe ()
        {
            var text = GetComponent<Text>();

            if( text == null )
            {
#if ENABLE_MAILCHIMP_ERROR_LOG
                Debug.LogError( "MailChimp — No UI Text found at this GameObject" );
#endif
                SubscribeError.Invoke( null );
                return;
            }

            Subscribe( text.text );
        }

        public void Subscribe ( string email )
        {
            try
            {
                var url = BuildUrl();
                if( string.IsNullOrEmpty( url ) )
                {
#if ENABLE_MAILCHIMP_ERROR_LOG
                    Debug.LogError( "MailChimp — Invalid API Key format" );
#endif
                    SubscribeError.Invoke( email );
                    return;
                }

                var mailAddress = new MailAddress( email );

                StartCoroutine( SendToMailChimp( url, mailAddress.Address ) );
            }
            catch( Exception ex )
            {
#if ENABLE_MAILCHIMP_ERROR_LOG
                Debug.LogError( "MailChimp — Invalid email: " + ex.Message );
#endif
                SubscribeError.Invoke( email );
            }
        }

        private string BuildUrl ()
        {
            var separatorIndex = _apiKey.LastIndexOf( '-' );
            if( separatorIndex < 0 )
                return null;

            var urlPrefix = _apiKey.Substring( separatorIndex + 1 );
            if( string.IsNullOrEmpty( urlPrefix ) )
                return null;

            return string.Format( UrlFormat, urlPrefix, _listId );
        }

        private IEnumerator SendToMailChimp ( string url, string email )
        {
            var request = new UnityWebRequest( url, UnityWebRequest.kHttpVerbPOST );

            request.SetRequestHeader( "Content-Type", "application/json" );
            request.SetRequestHeader( "Authorization", "apikey " + _apiKey );
            
            var json = string.Format( JsonFormat, email );
            var jsonBytes = Encoding.UTF8.GetBytes( json );
            request.uploadHandler = new UploadHandlerRaw( jsonBytes );

            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if( request.isNetworkError )
            {
#if ENABLE_MAILCHIMP_ERROR_LOG
                Debug.LogErrorFormat( "MailChimp — Network error: {0}", request.error );
#endif
                SubscribeError.Invoke( email );
                yield break;
            }

            if( request.isHttpError )
            {
#if ENABLE_MAILCHIMP_ERROR_LOG
                Debug.LogErrorFormat( "MailChimp — Subscribe error {0}: {1}\n{2}",
                    request.responseCode, request.error, request.downloadHandler.text );
#endif
                SubscribeError.Invoke( email );
                yield break;
            }

#if ENABLE_MAILCHIMP_SUCCESS_LOG
            Debug.LogFormat( "MailChimp — Subscribe success {0}\n{1}", request.responseCode, request.downloadHandler.text );
#endif
            SubscribeSuccess.Invoke( email );
        }

        [Serializable]
        public class MailChimpEvent : UnityEvent<string>
        {
        }
    }
}
