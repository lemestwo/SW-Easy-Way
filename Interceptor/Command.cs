using System.Diagnostics.CodeAnalysis;

namespace SW_Easy_Way.Interceptor
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum CommandPacket
	{
		BattleArenaResult,
		BattleArenaStart,
		BattleDungeonStart,
		BattleDungeonResult,
		CheckDarkPortalStatus,
		CheckLoginBlock,
		GetArenaLog,
		GetArenaUnitList,
		GetArenaWizardList,
		GetBlackMarketList,
		GetChatServerInfo,
		GetCostumeCollectionList,
		GetDailyQuests,
		GetEventTimeTable,
		GetFriendRequest,
		GetGuildInfo,
		GetGuildWarMatchupInfo,
		GetGuildWarParticipationInfo,
		GetGuildWarStatusInfo,
		GetMailList,
		GetNoticeChat,
		GetNoticeDungeon,
		GetRtpvpFavoriteList,
		GetRTPvPInfo_v3,
		getRtpvpRejoinInfo,
		getUnitUpgradeRewardInfo,
		GetWorldBossStatus,
		Harvest,
		HubUserLogin,
		receiveDailyRewardInactive,
		ReceiveDailyRewardSpecial,
		WorldRanking,
		WriteClientLog
	}
}