using System.Data;
using System.IO;
using System.Net.Mail;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace HorseCare;

public partial class MainWindow : Window
{
    private int? _ownerToSelectAfterReload;

    public MainWindow()
    {
        InitializeComponent();
        TrainingDatePicker.SelectedDate = DateTime.Today;
        VisitDatePicker.SelectedDate = DateTime.Today;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RunDatabaseActionAsync(LoadDataAsync);
    }

    private void ShowOverview_Click(object sender, RoutedEventArgs e) => NavigateTo(0, "Tổng quan");

    private void ShowHorses_Click(object sender, RoutedEventArgs e) => NavigateTo(1, "Hồ sơ ngựa");

    private void ShowTraining_Click(object sender, RoutedEventArgs e) => NavigateTo(2, "Lịch huấn luyện");

    private void ShowHealth_Click(object sender, RoutedEventArgs e) => NavigateTo(3, "Sức khỏe & thú y");

    private void NavigateTo(int pageIndex, string title)
    {
        Pages.SelectedIndex = pageIndex;
        PageTitle.Text = title;
    }

    private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyRolePermissions();
        }
    }

    private void ApplyRolePermissions()
    {
        var role = CurrentRole;
        AddHorseButton.IsEnabled = role is "Admin" or "Owner";
        AddOwnerButton.IsEnabled = role == "Admin";
        AddTrainingButton.IsEnabled = role == "Trainer";
        AddHealthButton.IsEnabled = role == "Doctor";
        OwnerComboBox.IsEnabled = role == "Admin";

        if (role == "Owner" && OwnerComboBox.Items.Count > 0)
        {
            OwnerComboBox.SelectedValue = GetCurrentOwnerId();
        }
    }

    private string CurrentRole =>
        (RoleComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Admin";

    private int? GetCurrentOwnerId()
    {
        if (OwnerComboBox.ItemsSource is not IEnumerable<PersonOption> owners)
        {
            return null;
        }

        return owners.FirstOrDefault(owner => owner.Role == "Owner")?.UserId;
    }

    private async Task LoadDataAsync()
    {
        await using var connection = Database.CreateConnection();
        await connection.OpenAsync();

        var horses = await ReadTableAsync(connection, """
            SELECT h.HorseId AS [Mã], h.Name AS [Tên ngựa], h.Breed AS [Giống],
                   h.Sex AS [Giới tính], h.DateOfBirth AS [Ngày sinh],
                   COALESCE(u.DisplayName, N'Chưa gán') AS [Chủ ngựa], h.Notes AS [Ghi chú]
            FROM dbo.Horses AS h
            LEFT JOIN dbo.AppUsers AS u ON u.UserId = h.OwnerUserId
            ORDER BY h.Name;
            """);
        var training = await ReadTableAsync(connection, """
            SELECT t.TrainingSessionId AS [Mã], h.Name AS [Ngựa], t.ScheduledAt AS [Ngày tập],
                   t.Activity AS [Nội dung], t.DurationMinutes AS [Thời lượng (phút)],
                   u.DisplayName AS [Huấn luyện viên], t.Status AS [Trạng thái], t.Notes AS [Ghi chú]
            FROM dbo.TrainingSessions AS t
            INNER JOIN dbo.Horses AS h ON h.HorseId = t.HorseId
            INNER JOIN dbo.AppUsers AS u ON u.UserId = t.TrainerUserId
            ORDER BY t.ScheduledAt DESC;
            """);
        var health = await ReadTableAsync(connection, """
            SELECT m.MedicalRecordId AS [Mã], h.Name AS [Ngựa], m.VisitDate AS [Ngày khám],
                   m.Diagnosis AS [Chẩn đoán], m.Treatment AS [Điều trị],
                   u.DisplayName AS [Bác sĩ], m.Status AS [Tình trạng]
            FROM dbo.MedicalRecords AS m
            INNER JOIN dbo.Horses AS h ON h.HorseId = m.HorseId
            INNER JOIN dbo.AppUsers AS u ON u.UserId = m.DoctorUserId
            ORDER BY m.VisitDate DESC;
            """);
        var owners = await ReadTableAsync(connection, """
            SELECT u.UserId AS [Mã], u.DisplayName AS [Chủ sở hữu],
                   u.UserName AS [Email], COUNT(h.HorseId) AS [Số ngựa]
            FROM dbo.AppUsers AS u
            LEFT JOIN dbo.Horses AS h ON h.OwnerUserId = u.UserId
            WHERE u.Role = N'Owner'
            GROUP BY u.UserId, u.DisplayName, u.UserName
            ORDER BY u.DisplayName;
            """);

        HorsesGrid.ItemsSource = horses.DefaultView;
        OwnersGrid.ItemsSource = owners.DefaultView;
        TrainingGrid.ItemsSource = training.DefaultView;
        HealthGrid.ItemsSource = health.DefaultView;
        HorseCountText.Text = horses.Rows.Count.ToString();
        TrainingCountText.Text = training.Rows.Count.ToString();
        HealthCountText.Text = health.Rows.Count.ToString();

        var ownerOptions = owners.Rows.Cast<DataRow>()
            .Select(row => new PersonOption(
                row.Field<int>("Mã"),
                row.Field<string>("Chủ sở hữu") ?? string.Empty,
                "Owner"))
            .ToList();
        var currentOwnerId = (OwnerComboBox.SelectedItem as PersonOption)?.UserId;
        OwnerComboBox.SelectedValuePath = nameof(PersonOption.UserId);
        OwnerComboBox.ItemsSource = ownerOptions;
        var preferredOwnerId = _ownerToSelectAfterReload
            ?? currentOwnerId;
        _ownerToSelectAfterReload = null;
        if (preferredOwnerId is int selectedOwnerId
            && ownerOptions.Any(owner => owner.UserId == selectedOwnerId))
        {
            OwnerComboBox.SelectedValue = selectedOwnerId;
        }
        else if (ownerOptions.Count > 0)
        {
            OwnerComboBox.SelectedIndex = 0;
        }

        var horseOptions = horses.Rows.Cast<DataRow>()
            .Select(row => new HorseOption(
                row.Field<int>("Mã"),
                row.Field<string>("Tên ngựa") ?? string.Empty))
            .ToList();
        TrainingHorseComboBox.SelectedValuePath = nameof(HorseOption.HorseId);
        HealthHorseComboBox.SelectedValuePath = nameof(HorseOption.HorseId);
        TrainingHorseComboBox.ItemsSource = horseOptions;
        HealthHorseComboBox.ItemsSource = horseOptions;
        if (horseOptions.Count > 0)
        {
            TrainingHorseComboBox.SelectedIndex = 0;
            HealthHorseComboBox.SelectedIndex = 0;
        }

        ApplyRolePermissions();
        StatusText.Text = "Đã kết nối SQL Server • Dữ liệu được lưu trực tiếp vào HorseCareDb.";
    }

    private async void AddOwner_Click(object sender, RoutedEventArgs e)
    {
        var name = OwnerNameBox.Text.Trim();
        var email = OwnerEmailBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowValidationMessage("Vui lòng nhập họ tên chủ sở hữu.");
            return;
        }

        if (!MailAddress.TryCreate(email, out var mailAddress)
            || !string.Equals(mailAddress.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            ShowValidationMessage("Vui lòng nhập địa chỉ email hợp lệ.");
            return;
        }

        await RunDatabaseActionAsync(async () =>
        {
            await using var connection = Database.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
                INSERT INTO dbo.AppUsers (DisplayName, UserName, Role)
                OUTPUT INSERTED.UserId
                VALUES (@DisplayName, @Email, N'Owner');
                """, connection);
            command.Parameters.Add("@DisplayName", SqlDbType.NVarChar, 100).Value = name;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 120).Value = email;
            var insertedId = await command.ExecuteScalarAsync();
            if (insertedId is not int ownerId)
            {
                throw new InvalidOperationException("SQL Server không trả về mã chủ sở hữu vừa tạo.");
            }

            _ownerToSelectAfterReload = ownerId;
            OwnerNameBox.Clear();
            OwnerEmailBox.Clear();
            await ReloadAfterSaveAsync("Đã tạo chủ sở hữu và thêm vào danh sách.");
        });
    }

    private static async Task<DataTable> ReadTableAsync(SqlConnection connection, string query)
    {
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    private async void AddHorse_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(HorseNameBox.Text))
        {
            ShowValidationMessage("Vui lòng nhập tên ngựa.");
            return;
        }

        await RunDatabaseActionAsync(async () =>
        {
            var ownerId = CurrentRole == "Owner"
                ? await FindRoleUserIdAsync("Owner")
                : (OwnerComboBox.SelectedItem as PersonOption)?.UserId;
            if (ownerId is null)
            {
                ShowValidationMessage("Cần có tài khoản Chủ ngựa trong cơ sở dữ liệu trước khi lưu.");
                return;
            }

            await using var connection = Database.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
                INSERT INTO dbo.Horses (Name, Breed, Sex, DateOfBirth, OwnerUserId, Notes)
                VALUES (@Name, @Breed, @Sex, @DateOfBirth, @OwnerUserId, @Notes);
                """, connection);
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = HorseNameBox.Text.Trim();
            command.Parameters.Add("@Breed", SqlDbType.NVarChar, 80).Value = OptionalText(BreedBox.Text);
            command.Parameters.Add("@Sex", SqlDbType.NVarChar, 20).Value =
                (SexComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chưa rõ";
            command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value =
                BirthDatePicker.SelectedDate is DateTime birthDate ? birthDate.Date : DBNull.Value;
            command.Parameters.Add("@OwnerUserId", SqlDbType.Int).Value = ownerId.Value;
            command.Parameters.Add("@Notes", SqlDbType.NVarChar, 500).Value = OptionalText(HorseNotesBox.Text);
            await command.ExecuteNonQueryAsync();

            HorseNameBox.Clear();
            BreedBox.Clear();
            HorseNotesBox.Clear();
            BirthDatePicker.SelectedDate = null;
            await ReloadAfterSaveAsync("Đã lưu hồ sơ ngựa.");
        });
    }

    private async void AddTraining_Click(object sender, RoutedEventArgs e)
    {
        if (TrainingHorseComboBox.SelectedValue is not int horseId
            || TrainingDatePicker.SelectedDate is not DateTime scheduledAt
            || string.IsNullOrWhiteSpace(ActivityBox.Text))
        {
            ShowValidationMessage("Vui lòng chọn ngựa, ngày tập và nhập nội dung buổi tập.");
            return;
        }

        if (!int.TryParse(DurationBox.Text, out var duration) || duration <= 0)
        {
            ShowValidationMessage("Thời lượng buổi tập phải là số phút lớn hơn 0.");
            return;
        }

        await RunDatabaseActionAsync(async () =>
        {
            var trainerId = await FindRoleUserIdAsync("Trainer");
            if (trainerId is null)
            {
                ShowValidationMessage("Cần có tài khoản Huấn luyện viên trong cơ sở dữ liệu trước khi lưu.");
                return;
            }

            await using var connection = Database.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
                INSERT INTO dbo.TrainingSessions
                    (HorseId, TrainerUserId, ScheduledAt, Activity, DurationMinutes, Status, Notes)
                VALUES
                    (@HorseId, @TrainerUserId, @ScheduledAt, @Activity, @DurationMinutes, N'Đã lên lịch', @Notes);
                """, connection);
            command.Parameters.Add("@HorseId", SqlDbType.Int).Value = horseId;
            command.Parameters.Add("@TrainerUserId", SqlDbType.Int).Value = trainerId.Value;
            command.Parameters.Add("@ScheduledAt", SqlDbType.DateTime2).Value = scheduledAt.Date;
            command.Parameters.Add("@Activity", SqlDbType.NVarChar, 200).Value = ActivityBox.Text.Trim();
            command.Parameters.Add("@DurationMinutes", SqlDbType.Int).Value = duration;
            command.Parameters.Add("@Notes", SqlDbType.NVarChar, 500).Value = OptionalText(TrainingNotesBox.Text);
            await command.ExecuteNonQueryAsync();

            ActivityBox.Clear();
            TrainingNotesBox.Clear();
            DurationBox.Text = "60";
            await ReloadAfterSaveAsync("Đã lưu lịch huấn luyện.");
        });
    }

    private async void AddHealth_Click(object sender, RoutedEventArgs e)
    {
        if (HealthHorseComboBox.SelectedValue is not int horseId
            || VisitDatePicker.SelectedDate is not DateTime visitDate
            || string.IsNullOrWhiteSpace(DiagnosisBox.Text))
        {
            ShowValidationMessage("Vui lòng chọn ngựa, ngày khám và nhập chẩn đoán.");
            return;
        }

        await RunDatabaseActionAsync(async () =>
        {
            var doctorId = await FindRoleUserIdAsync("Doctor");
            if (doctorId is null)
            {
                ShowValidationMessage("Cần có tài khoản Bác sĩ thú y trong cơ sở dữ liệu trước khi lưu.");
                return;
            }

            await using var connection = Database.CreateConnection();
            await connection.OpenAsync();
            await using var command = new SqlCommand("""
                INSERT INTO dbo.MedicalRecords
                    (HorseId, DoctorUserId, VisitDate, Diagnosis, Treatment, Status)
                VALUES
                    (@HorseId, @DoctorUserId, @VisitDate, @Diagnosis, @Treatment, @Status);
                """, connection);
            command.Parameters.Add("@HorseId", SqlDbType.Int).Value = horseId;
            command.Parameters.Add("@DoctorUserId", SqlDbType.Int).Value = doctorId.Value;
            command.Parameters.Add("@VisitDate", SqlDbType.Date).Value = visitDate.Date;
            command.Parameters.Add("@Diagnosis", SqlDbType.NVarChar, 500).Value = DiagnosisBox.Text.Trim();
            command.Parameters.Add("@Treatment", SqlDbType.NVarChar, 1000).Value = OptionalText(TreatmentBox.Text);
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 40).Value =
                (HealthStatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Đang theo dõi";
            await command.ExecuteNonQueryAsync();

            DiagnosisBox.Clear();
            TreatmentBox.Clear();
            await ReloadAfterSaveAsync("Đã lưu hồ sơ sức khỏe.");
        });
    }

    private async Task ReloadAfterSaveAsync(string successMessage)
    {
        await LoadDataAsync();
        StatusText.Text = successMessage;
        MessageBox.Show(this, successMessage, "StableCare", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static async Task<int?> FindRoleUserIdAsync(string role)
    {
        await using var connection = Database.CreateConnection();
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            SELECT TOP (1) UserId
            FROM dbo.AppUsers
            WHERE Role = @Role
            ORDER BY UserId;
            """, connection);
        command.Parameters.Add("@Role", SqlDbType.NVarChar, 20).Value = role;
        var result = await command.ExecuteScalarAsync();
        return result is int userId ? userId : null;
    }

    private async Task RunDatabaseActionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (SqlException exception)
        {
            ShowDatabaseError(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            ShowDatabaseError(exception.Message);
        }
        catch (IOException exception)
        {
            ShowDatabaseError(exception.Message);
        }
        catch (JsonException exception)
        {
            ShowDatabaseError(exception.Message);
        }
    }

    private void ShowDatabaseError(string details)
    {
        var message = "Không thể hoàn tất thao tác với SQL Server.\n"
            + "Hãy chạy Database\\CreateHorseCareDb.sql và kiểm tra chuỗi kết nối trong appsettings.json.\n\n"
            + details;
        StatusText.Text = "Lỗi kết nối hoặc truy vấn cơ sở dữ liệu.";
        MessageBox.Show(this, message, "Lỗi cơ sở dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowValidationMessage(string message) =>
        MessageBox.Show(this, message, "Kiểm tra dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);

    private static object OptionalText(string text) =>
        string.IsNullOrWhiteSpace(text) ? DBNull.Value : text.Trim();

    private sealed record PersonOption(int UserId, string DisplayName, string Role);

    private sealed record HorseOption(int HorseId, string Name);
}
