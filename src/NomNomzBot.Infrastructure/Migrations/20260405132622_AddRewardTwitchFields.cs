using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NoMercyBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardTwitchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Rewards_BroadcasterId", table: "Rewards");

            migrationBuilder.AddColumn<int>(
                name: "Cost",
                table: "Rewards",
                type: "integer",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "TwitchRewardId",
                table: "Rewards",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_BroadcasterId_TwitchRewardId",
                table: "Rewards",
                columns: new[] { "BroadcasterId", "TwitchRewardId" },
                unique: true,
                filter: "\"TwitchRewardId\" IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Rewards_BroadcasterId_TwitchRewardId",
                table: "Rewards"
            );

            migrationBuilder.DropColumn(name: "Cost", table: "Rewards");

            migrationBuilder.DropColumn(name: "TwitchRewardId", table: "Rewards");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_BroadcasterId",
                table: "Rewards",
                column: "BroadcasterId"
            );
        }
    }
}
