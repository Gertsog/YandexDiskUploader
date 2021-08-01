namespace YandexDiskUploader
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataFolder = "C:\\Users\\Admin\\Desktop\\TEST\\";
            var yandexDiskFolder = "TestFolder";

            //Можно получить по ссылке: https://yandex.ru/dev/disk/poligon/#
            AsyncFilesUploader.AuthToken = "";

            AsyncFilesUploader.AllowFileOverwriting = true;
            AsyncFilesUploader.MaxParallelUploadingFiles = 20;
            AsyncFilesUploader.UploadFiles(dataFolder, yandexDiskFolder);
			
			Console.ReadKey();
        }
    }
}