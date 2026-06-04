using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentaSchedule.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovedAppointmentUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_AppointmentDateTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Doctor_Time_Approved",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDateTime" },
                unique: true,
                filter: "[Status] = 'Approved'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_Doctor_Time_Approved",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDateTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "AppointmentDateTime" });
        }
    }
}
