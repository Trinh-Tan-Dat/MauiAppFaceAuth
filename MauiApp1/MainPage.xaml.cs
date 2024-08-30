using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Microsoft.Maui.Controls;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Collections.Generic;

namespace MauiApp1
{
	public partial class MainPage : ContentPage
	{
		private VideoCapture _capture;
		private CascadeClassifier _faceClassifier;
		private EigenFaceRecognizer _faceRecognizer;
		private List<Mat> _registeredFaces = new List<Mat>();
		private List<int> _userIds = new List<int>();
		private int _imageWidth = 100; // Kích thước ảnh huấn luyện
		private int _imageHeight = 100; // Kích thước ảnh huấn luyện

		public MainPage()
		{
			InitializeComponent();
			InitializeCamera();
			InitializeRecognizer();
		}

		private void InitializeCamera()
		{
			try
			{
				_capture = new VideoCapture(0); // 0 là camera mặc định
				_faceClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml");
			}
			catch (Exception ex)
			{
				DisplayAlert("Error", $"Lỗi khi khởi tạo camera: {ex.Message}", "OK");
			}
		}

		private void InitializeRecognizer()
		{
			// Khởi tạo Face Recognizer
			_faceRecognizer = new EigenFaceRecognizer();
		}

		private async void OnRegisterFaceClicked(object sender, EventArgs e)
		{
			if (_capture != null && _capture.IsOpened && !string.IsNullOrEmpty(UserIdEntry.Text))
			{
				if (!int.TryParse(UserIdEntry.Text, out int userId))
				{
					await DisplayAlert("Error", "User ID phải là số nguyên.", "OK");
					return;
				}

				Mat frame = new Mat();
				_capture.Read(frame);

				if (!frame.IsEmpty)
				{
					var grayImage = frame.ToImage<Gray, byte>();
					var faces = _faceClassifier.DetectMultiScale(grayImage, 1.1, 4);

					if (faces.Length > 0)
					{
						// Lấy khuôn mặt đầu tiên để đăng ký
						var face = new Mat(grayImage.Mat, faces[0]);
						var resizedFace = new Mat();
						CvInvoke.Resize(face, resizedFace, new System.Drawing.Size(_imageWidth, _imageHeight));

						// Lưu khuôn mặt và userId vào danh sách
						_registeredFaces.Add(resizedFace);
						_userIds.Add(userId);

						await DisplayAlert("Success", $"Đã đăng ký khuôn mặt cho User ID: {userId}", "OK");
					}
					else
					{
						await DisplayAlert("Error", "Không tìm thấy khuôn mặt nào.", "OK");
					}
				}
			}
		}

		private async void OnDetectFaceClicked(object sender, EventArgs e)
		{
			if (_capture != null && _capture.IsOpened)
			{
				Mat frame = new Mat();
				_capture.Read(frame);

				if (!frame.IsEmpty)
				{
					var grayImage = frame.ToImage<Gray, byte>();
					var faces = _faceClassifier.DetectMultiScale(grayImage, 1.1, 4);
					var image = frame.ToImage<Bgr, byte>();

					foreach (var rect in faces)
					{
						var detectedFace = new Mat(grayImage.Mat, rect);
						var resizedFace = new Mat();
						CvInvoke.Resize(detectedFace, resizedFace, new System.Drawing.Size(_imageWidth, _imageHeight));

						image.Draw(rect, new Bgr(0, 255, 0), 2);

						if (_registeredFaces.Count > 0)
						{
							_faceRecognizer.Train(_registeredFaces.ToArray(), _userIds.ToArray());
							var result = _faceRecognizer.Predict(resizedFace);

							if (result.Label != -1)
							{
								string text = $"ID: {result.Label}, Confidence: {result.Distance:F2}";
								CvInvoke.PutText(image, text, new System.Drawing.Point(rect.X, rect.Y - 10),
									Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new Bgr(0, 255, 0).MCvScalar);
							}
							else
							{
								CvInvoke.PutText(image, "Unknown", new System.Drawing.Point(rect.X, rect.Y - 10),
									Emgu.CV.CvEnum.FontFace.HersheySimplex, 0.5, new Bgr(0, 0, 255).MCvScalar);
							}
						}
					}

					CameraView.Source = ConvertImageToImageSource(image);
				}
			}
		}

		private ImageSource ConvertImageToImageSource(Image<Bgr, byte> image)
		{
			using (var ms = new MemoryStream())
			{
				using (var imgSharp = SixLabors.ImageSharp.Image.LoadPixelData<Bgr24>(image.Bytes, image.Width, image.Height))
				{
					imgSharp.Save(ms, new PngEncoder());
					ms.Seek(0, SeekOrigin.Begin);
					return ImageSource.FromStream(() => new MemoryStream(ms.ToArray()));
				}
			}
		}
	}
}
