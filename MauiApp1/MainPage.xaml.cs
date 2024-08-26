using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace MauiApp1;

public partial class MainPage : ContentPage
{
	private static readonly string apiUrl = "http://103.9.157.149:32168/v1/vision/face/recognize";

	public MainPage()
	{
		InitializeComponent();
	}

	public async void OpenCamera(object sender, EventArgs e)
	{
		try
		{
			var photo = await MediaPicker.CapturePhotoAsync();

			if (photo != null)
			{
				var stream = await photo.OpenReadAsync();
				var filePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

				using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
				{
					await stream.CopyToAsync(fileStream);
				}

				var result = await SendPhotoToApi(filePath);

				await DisplayAlert("Kết quả nhận diện khuôn mặt", result, "OK");

				var imageSource = ImageSource.FromStream(() => stream);
				var image = new Image
				{
					Source = imageSource,
					WidthRequest = 300,
					HeightRequest = 300
				};

				MainLayout.Children.Add(image);
			}
		}
		catch (FeatureNotSupportedException)
		{
			await DisplayAlert("Alert", "Not support", "OK");
		}
		catch (PermissionException)
		{
			await DisplayAlert("Alert", "Not permission", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
		}
	}

	private async Task<string> SendPhotoToApi(string filePath)
	{
		try
		{
			using (var client = new HttpClient())
			using (var content = new MultipartFormDataContent())
			{
				var fileContent = new ByteArrayContent(File.ReadAllBytes(filePath));
				fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");

				content.Add(fileContent, "image", Path.GetFileName(filePath));

				var response = await client.PostAsync(apiUrl, content);
				response.EnsureSuccessStatusCode();

				var jsonResponse = await response.Content.ReadAsStringAsync();
				var result = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

				return JsonConvert.SerializeObject(result, Formatting.Indented); // Định dạng JSON cho dễ đọc
			}
		}
		catch (Exception ex)
		{
			return $"Error: {ex.Message}";
		}
	}
}

