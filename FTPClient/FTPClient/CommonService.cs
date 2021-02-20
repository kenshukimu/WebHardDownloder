using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web;

namespace FTPClient
{
    public class CommonService
    {
        //FTP정보
        //protected static String ftpPath = @"ftp://webdisk.korcham.net";
        //protected static String ftpUser = "webFTP";
        //protected static String ftpPass = "111111";
             
        //protected static int ftpPort = 21;

        public FtpWebResponse Connect(String url, string method, ref long fws, String ftpPath, String ftpUser, String ftpPass)
        {
            // WebRequest 클래스를 이용해 접속하기 때문에 객체를 가져온다. (FtpWebRequest로 변환)
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath + url);
            
            // Binary 형식으로 사용한다.
            request.UseBinary = true;
           
            if (!method.Equals(System.Net.WebRequestMethods.Ftp.ListDirectory))
            {
                request.UsePassive = true;
                request.KeepAlive = true;

                //파일사이즈 체크
                request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = new NetworkCredential(ftpUser, ftpPass);

                fws = request.GetResponse().ContentLength;
                request.GetResponse().Close();

                request = (FtpWebRequest)WebRequest.Create(ftpPath + url);

                //request.Method = WebRequestMethods.Ftp.GetFileSize;
                request.Credentials = new NetworkCredential(ftpUser, ftpPass);
            }
            else
            {
                request.UsePassive = false;
                request.KeepAlive = false;
            }

            // 로그인 인증
            request.Credentials = new NetworkCredential(ftpUser, ftpPass);
            // FTP 메소드 설정(아래에 별도 설명)
            request.Method = method;
          
            // 접속해서 WebResponse함수를 가져온다.
            return request.GetResponse() as FtpWebResponse;
        }       
    }
}
