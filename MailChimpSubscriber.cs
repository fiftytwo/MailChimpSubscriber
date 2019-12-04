using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Net.Mail;


namespace Fiftytwo
{
    public class MailChimpSubscriber : MonoBehaviour
    {
        private const string UrlFormat = "https://{0}.api.mailchimp.com/3.0/lists/{1}/members";
        private const string DataFormat = "{{\"email_address\":\"{0}\", \"status\":\"subscribed\"}}";

        [SerializeField]
        private string _apiKey;
        [SerializeField]
        private string _listId;

        [SerializeField]
        private UnityEvent _subscribeSuccess;
        [SerializeField]
        private UnityEvent _subscribeError;


        public void Subscribe ()
        {
            var text = GetComponent<Text>();

            if( text == null )
            {
                Debug.LogError( "MailChimp — No UI Text found at this GameObject" );
                return;
            }
            else
            {
                Subscribe( text.text );
            }
        }

        public void Subscribe ( string email )
        {
            if( IsValidEmail( email ) )
            {
                var request = BuildRequest( email );

                if( request != null )
                {
                    StartCoroutine( SendToMailChimp( request ));
                }
                else
                {
                    _subscribeError.Invoke();
                }
            }
            else
            {
                Debug.Log( "MailChimp — Invalid email" );
                _subscribeError.Invoke();
            }
        }

        private bool IsValidEmail ( string email )
        {
            if( string.IsNullOrEmpty( email ) )
                return false;

            try
            {
                new MailAddress( email );

                return true;
            }
            catch( FormatException )
            {
                return false;
            }
        }

        private IEnumerator SendToMailChimp ( UnityWebRequest request )
        {
            yield return request;

            if( string.IsNullOrEmpty( request.error ) )
            {
                Debug.Log( "MailChimp — Subscribe success" );
                _subscribeSuccess.Invoke();
            }
            else
            {
                Debug.Log( "MailChimp — Subscribe error: " + request.error );
                _subscribeError.Invoke();
            }
        }

        private UnityWebRequest BuildRequest ( string email )
        {
            var data = string.Format( DataFormat, email );

            var splittedApiKey = _apiKey.Split( '-' );

            if( splittedApiKey.Length != 2 )
            {
                Debug.LogError( "MailChimp — Invalid API Key format" );
                return null;
            }

            var urlPrefix = splittedApiKey[1];

            var url = string.Format( UrlFormat, urlPrefix, _listId );

            var webRequest = UnityWebRequest.Post( url, data );
            webRequest.SetRequestHeader( "Authorization", "apikey " + _apiKey );

            return webRequest;
        }
    }
}
