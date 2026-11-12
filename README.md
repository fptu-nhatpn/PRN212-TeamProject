# StableCare - PRN212

Ứng dụng desktop WPF/C# quản lý chăm sóc ngựa, sử dụng SQL Server. Giao diện dùng tông tím và có các màn hình tổng quan, hồ sơ ngựa, lịch huấn luyện và hồ sơ sức khỏe.

## Yêu cầu

- Visual Studio 2022 với workload **.NET desktop development**.
- .NET 8 SDK.
- SQL Server Express hoặc SQL Server Developer đang chạy trên máy.

## Tạo database và chạy

1. Trong SSMS, kết nối bằng **Windows Authentication** tới server `LAPTOP-2S8NVM8A`, mở `Database\CreateHorseCareDb.sql`, rồi nhấn **Execute**. Script tạo `HorseCareDb`, các bảng và dữ liệu mẫu gồm nhiều chủ sở hữu/ngựa. Có thể chạy lại script an toàn để cập nhật dữ liệu mẫu.
   - Có thể chạy từ PowerShell: `sqlcmd -S LAPTOP-2S8NVM8A -E -i Database\CreateHorseCareDb.sql`
2. Mở `HorseCare.sln` bằng Visual Studio.
3. Nếu SQL Server dùng tên máy/instance khác, sửa `ConnectionStrings.DefaultConnection` trong `appsettings.json`. Cấu hình mặc định khớp server `LAPTOP-2S8NVM8A` trong SSMS và dùng Windows Authentication.
4. Chọn cấu hình Debug rồi nhấn **F5**. NuGet sẽ khôi phục `Microsoft.Data.SqlClient` khi build lần đầu.

## Vai trò demo

Chọn vai trò ở góc trên bên phải để thử luồng tương ứng: Quản trị viên tạo chủ sở hữu và thêm hồ sơ ngựa, Chủ ngựa thêm ngựa, Huấn luyện viên lập lịch tập, Bác sĩ thú y ghi hồ sơ khám. Danh sách chủ sở hữu và số ngựa được hiển thị ở màn hình hồ sơ ngựa. Vai trò này chỉ phục vụ demo giao diện, không phải đăng nhập hay cơ chế bảo mật.
