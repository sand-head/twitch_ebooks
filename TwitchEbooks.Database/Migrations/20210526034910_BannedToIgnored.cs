using Microsoft.EntityFrameworkCore.Migrations;

namespace TwitchEbooks.Database.Migrations
{
    public partial class BannedToIgnored : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BannedTwitchUsers_Channels_ChannelId",
                table: "BannedTwitchUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BannedTwitchUsers",
                table: "BannedTwitchUsers");

            migrationBuilder.RenameTable(
                name: "BannedTwitchUsers",
                newName: "IgnoredUsers");

            migrationBuilder.RenameIndex(
                name: "IX_BannedTwitchUsers_ChannelId",
                table: "IgnoredUsers",
                newName: "IX_IgnoredUsers_ChannelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_IgnoredUsers",
                table: "IgnoredUsers",
                columns: new[] { "Id", "ChannelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_IgnoredUsers_Channels_ChannelId",
                table: "IgnoredUsers",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IgnoredUsers_Channels_ChannelId",
                table: "IgnoredUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_IgnoredUsers",
                table: "IgnoredUsers");

            migrationBuilder.RenameTable(
                name: "IgnoredUsers",
                newName: "BannedTwitchUsers");

            migrationBuilder.RenameIndex(
                name: "IX_IgnoredUsers_ChannelId",
                table: "BannedTwitchUsers",
                newName: "IX_BannedTwitchUsers_ChannelId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BannedTwitchUsers",
                table: "BannedTwitchUsers",
                columns: new[] { "Id", "ChannelId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BannedTwitchUsers_Channels_ChannelId",
                table: "BannedTwitchUsers",
                column: "ChannelId",
                principalTable: "Channels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
