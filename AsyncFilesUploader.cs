using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace YandexDiskUploader
{
    static class AsyncFilesUploader
    {
        /// <summary>
        /// Корневой URL для запросов к API Яндекс.Диска
        /// </summary>
        private static string baseUrl = "https://cloud-api.yandex.net/v1/disk/";

        /// <summary>
        /// Количество неудачных загрузок
        /// </summary>
        private static int failedLoadings = 0;

        /// <summary>
        /// Токен авторизации, индивидуален для каждого пользователя
        /// </summary>
        private static string authToken;
        public static string AuthToken { get => authToken; set => authToken = value; }

        /// <summary>
        /// Максимальное количество файлов, загружаемых одновременно
        /// </summary>
        private static int maxParallelUploadingFiles = 10;
        public static int MaxParallelUploadingFiles { get => maxParallelUploadingFiles; set => maxParallelUploadingFiles = value; }

        /// <summary>
        /// Разрешение на перезапись существующих файлов
        /// </summary>
        private static bool allowFileOverwriting = false;
        public static bool AllowFileOverwriting { get => allowFileOverwriting; set => allowFileOverwriting = value; }

        /// <summary>
        /// Метод, осуществляющий загрузку файлов с компьютера в Яндекс.Диск
        /// </summary>
        /// <param name="localFolder">
        /// Локальная папка, где хранятся файлы, которые нужно загрузить на Яндекс.Диск
        /// </param>
        /// <param name="diskFolder">
        /// Папка на Яндекс.Диске, куда должны быть загружены файлы
        /// </param>
        public static void UploadFiles(string localFolder, string diskFolder) {
            try
            {
                // Получение списка файлов в локальной папке
                string[] localFiles = Directory.GetFiles(localFolder, "*");
                if (localFiles.Length == 0)
                    throw new Exception("Локальная папка пуста.");

                try
                {
                    // Запрос для создания новой папки на Яндекс.Диске
                    var createFolderRequestUrl = $"{baseUrl}resources?path={diskFolder}";
                    var createFolderResponse = createSimpleRequest(createFolderRequestUrl, "PUT");
                }
                catch (Exception)
                {
                    Console.WriteLine("Ошибка при создании папки на Яндекс.Диске.");
                    throw;
                }

                // Параллельная загрузка файлов из локальной папки
                Parallel.ForEach(localFiles, new ParallelOptions() { MaxDegreeOfParallelism = maxParallelUploadingFiles }, file =>
                {
                    var fileName = Path.GetFileName(file);
                    try
                    {
                        // Получение ссылки для последующей загрузки файла
                        var createFileRequestUrl = $"{baseUrl}resources/upload?path={diskFolder}%2F{fileName}&overwrite={allowFileOverwriting}";
                        var createFileResponse = createSimpleRequest(createFileRequestUrl, "GET");

                        string responseString;
                        using (var responseStream = createFileResponse.GetResponseStream())
                        {
                            responseString = new StreamReader(responseStream).ReadToEnd();
                        }
                        var uploadLink = JObject.Parse(responseString).Property("href")?.Value.ToString();

                        // Преобразование файла в стрим и загрузка на сервер
                        using (var fileStream = new FileStream(Path.GetFullPath(file), FileMode.Open, FileAccess.Read))
                        {
                            var fileUploadRequest = (HttpWebRequest)WebRequest.Create(uploadLink);
                            fileUploadRequest.Method = "PUT";
                            fileUploadRequest.Headers.Add("Authorization", authToken);
                            fileUploadRequest.KeepAlive = true;
                            fileUploadRequest.ContentLength = fileStream.Length;

                            var contentByteArray = new byte[fileStream.Length];
                            int bufferSize = fileStream.Length < 2048 ? (int)fileStream.Length : 2048;
                            int readedBytesLenght;

                            using (var fileUploadRequestStream = fileUploadRequest.GetRequestStream())
                            {
                                Console.WriteLine($"Файл {fileName} - загружается.");
                                while ((readedBytesLenght = fileStream.Read(contentByteArray, 0, bufferSize)) > 0)
                                {
                                    fileUploadRequestStream.Write(contentByteArray, 0, readedBytesLenght);
                                }
                                var fileUploadResponse = (HttpWebResponse)fileUploadRequest.GetResponse();
                                Console.WriteLine($"Файл {fileName} - загрузка завершена.");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        failedLoadings++;
                        if (e.Message.ToLower().Contains("(409) conflict"))
                            Console.WriteLine($"Файл с именем {fileName} уже существует.");
                        else
                            Console.WriteLine($"Файл {fileName} не может быть загружен.");
                    }
                });

                if (failedLoadings == 0) 
                {
                    Console.WriteLine("Все файлы загружены.");
                }
                else 
                {
                    var sucsessfullLoadedFiles = localFiles.Length - failedLoadings;
                    Console.WriteLine($"Загружено {sucsessfullLoadedFiles} из {localFiles.Length} файлов.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Передача файлов отменена.");
            }
        }

        // Метод для формирования простых похожих запросов
        private static WebResponse createSimpleRequest(string url, string method) {
            var webRequset = WebRequest.Create(url);
            webRequset.Method = method;
            webRequset.Headers.Add("Authorization", authToken);
            return webRequset.GetResponse();
        }
    }
}