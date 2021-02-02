using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using System.Text;
using Oxide.Core.Libraries;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("EasyVote", "Exel80", "2.0.4", ResourceId = 2102)]
    [Description("Simple and smooth voting start by activating one scirpt.")]
    public class EasyVote : RustPlugin
    {
        [PluginReference] private Plugin DiscordMessages;

        #region Initializing
        // Permissions
        private const bool DEBUG = false;
        private const string permUse = "EasyVote.Use";
        private const string permAdmin = "EasyVote.Admin";

        // Vote status arrays
        // 0 = Havent voted yet OR already claimed.
        // 1 = Voted and waiting claiming.
        // 2 = Already claimed reward (Far us i know, RustServers is only who use this response number)
        protected string[] voteStatus = { "No reward(s)", "Claim reward(s)", "Claim reward(s) / Already claimed?" };
        protected string[] voteStatusColor = { "red", "lime", "yellow" };

        // Spam protect list
        Dictionary<ulong, StringBuilder> claimCooldown = new Dictionary<ulong, StringBuilder>();
        Dictionary<ulong, bool> checkCooldown = new Dictionary<ulong, bool>();

        // List received reward(s) one big list.
        StringBuilder rewardsString = new StringBuilder();

        // List all vote sites.
        List<string> _availableAPISites = new List<string>();
        StringBuilder _voteList = new StringBuilder();
        private List<int> _numberMax = new List<int>();
        StringBuilder _helpYou = new StringBuilder();
        StringBuilder _helpAdmin = new StringBuilder();

        private void _ReloadEasyVote(bool WriteObject = true)
        {
            if (WriteObject)
            {
                Config.WriteObject(_config);
                PrintWarning($"Config saved successfully!");
            }

            // This is only used when ADMIN
            // reload EasyVote with EasyVote own command.
            LoadConfigValues();
            LoadMessages();

            _numberMax.Clear();
            BuildNumberMax();

            _availableAPISites.Clear();
            checkVoteSites();

            _voteList.Clear();
            voteList();
        }

        void Loaded()
        {
            // Load configs
            LoadConfigValues();
            LoadMessages();

            // Regitering permissions
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permAdmin, this);

            // Load storedata
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("EasyVote");

            // Check rewards and add them one big list
            BuildNumberMax();

            // Build helptext
            HelpText();

            // Check available vote sites
            checkVoteSites();

            // Build StringBuilders
            voteList();
        }
        #endregion

        #region Localization
        string _lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

         private void LoadMessages()
        {
			//English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command!",
                ["ClaimStatus"] = "<color=#00fff7>[{0}]</color> Checked {1}, Status: {2}",
                ["ClaimError"] = "Something went wrong! Player <color=#ff0000>{0} got an error</color> from <color=#fffb00>{1}</color>. Please try again later!",
                ["ClaimReward"] = "You just received your vote reward(s). Enjoy!",
                ["ClaimPleaseWait"] = "Checking the voting websites. Please wait...",
                ["VoteList"] = "You have voted <color=#fffb00>{1}</color> time(s)!\n Leave another vote on these websites:\n{0}",
                ["EarnReward"] = "When you have voted, type <color=#fffb00>/claim</color> to claim your reward(s)!",
                ["RewardListFirstTime"] = "<color=#00fff7>Reward for voting for the first time.</color>",
                ["RewardListEverytime"] = "<color=#00fff7>Reward, which player will receive everytime they vote.</color>",
                ["RewardList"] = "<color=#00fff7>Reward for voting</color> <color=#FFA500>{0}</color> <color=#00fff7>time(s).</color>",
                ["Received"] = "You have received {0}x {1}",
                ["ThankYou"] = "Thank you for voting! You have voted <color=#fffb00>{0}</color> time(s) Here is your reward for..\n{1}",
                ["NoRewards"] = "You do not have any new rewards available\n Please type <color=#fffb00>/vote</color> and go to one of the websites to vote and receive your reward",
                ["RemeberClaim"] = "You haven't claimed your reward from voting for the server yet! Use <color=#fffb00>/claim</color> to claim your reward!\n You have to claim your reward within <color=#fffb00>24h</color>! Otherwise it will be gone!",
                ["GlobalChatAnnouncments"] = "<color=#fffb00>{0}</color><color=#00fff7> has voted </color><color=#fffb00>{1}</color><color=#00fff7> time(s) and just received their rewards. Find out where you can vote by typing</color><color=#fffb00> /vote</color>\n<color=#00fff7>To see a list of avaliable rewards type</color><color=#fffb00> /rewardlist</color>",
                ["money"] = "<color=#fffb00>{0}$</color> has been desposited into your account",
                ["rp"] = "You have gained <color=#fffb00>{0}</color> reward points",
                ["tempaddgroup"] = "You have been temporality added to <color=#fffb00>{0}</color> group (Expire in {1})",
                ["tempgrantperm"] = "You have temporality granted <color=#fffb00>{0}</color> permission (Expire in {1})",
                ["zlvl-wc"] = "You have gained <color=#fffb00>{0}</color> woodcrafting level(s)",
                ["zlvl-m"] = "You have gained <color=#fffb00>{0}</color> mining level(s)",
                ["zlvl-s"] = "You have gained <color=#fffb00>{0}</color> skinning level(s)",
                ["zlvl-c"] = "You have gained <color=#fffb00>{0}</color> crafting level(s)",
                ["zlvl-*"] = "You have gained <color=#fffb00>{0}</color> in all level(s)",
                ["oxidegrantperm"] = "You have been granted <color=#fffb00>{0}</color> permission",
                ["oxiderevokeperm"] = "Your permission <color=#fffb00>{0}</color> has been revoked",
                ["oxidegrantgroup"] = "You have been added to <color=#fffb00>{0}</color> group",
                ["oxiderevokegroup"] = "You have been removed from <color=#fffb00>{0}</color> group"
            }, this, "en");
			//French
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["NoPermission"] = "Vous n'êtes pas autorisé à utiliser cette commande!",
				["ClaimStatus"] = "<color=#00fff7>[{0}]</color> Vérifié {1}, état: {2}",
				["ClaimError"] = "Quelque chose a mal tourné! Joueur <color=ff0000>{0} got an error</color> de <color=00fff7>{1}</color>. Veuillez réessayer plus tard!",
				["ClaimReward"] = "Vous venez de recevoir votre / vos récompense (s) de vote. Prendre plaisir!",
				["ClaimPleaseWait"] = "Vérification des sites Web de vote. S'il vous plaît, attendez...",
				["VoteList"] = "Vous avez voté <color=00fff7>{1}</color> temps!\n Laissez un autre vote sur ces sites:\n{0}",
				["EarnReward"] = "Lorsque vous avez voté, saisissez <color=00fff7>/claim</color> pour réclamer vos récompenses!",
				["RewardListFirstTime"] = "Récompense pour avoir voté pour la première fois.",
				["RewardListEverytime"] = "Récompense, quel joueur recevra à chaque fois qu'il vote.",
				["RewardList"] = "Récompense pour voter <color=FFA500>{0}</color> temps.",
				["Received"] = "Vous avez reçu {0}x {1}",
				["ThankYou"] = "Merci d'avoir voté! Vous avez voté <color=00fff7>{0}</color> temps (s) Voici votre récompense pour..\n{1}",
				["NoRewards"] = "Vous n'avez pas de nouvelles récompenses disponibles\n Veuillez saisir <color=00fff7>/vote</color> et allez sur l'un des sites pour voter et recevoir votre récompense",
				["RemeberClaim"] = "Vous n'avez pas encore réclamé votre récompense en votant pour le serveur! Utilisation <color=00fff7>/claim</color> réclamer votre récompense!\n Vous devez réclamer votre récompense dans <color=00fff7>24h</color>! Sinon, ce sera parti!",
				["GlobalChatAnnouncments"] = "<color=00fff7>{0}</color> a voté <color=00fff7>{1}</color> fois et viens de recevoir leurs récompenses. Découvrez où vous pouvez voter en tapant<color=00fff7> /vote</color>\nPour voir une liste de récompenses disponibles<color=00fff7> /rewardlist</color>",
				["money"] = "<color=00fff7>{0}$</color> a été déposé sur votre compte",
				["rp"] = "Vous avez gagné <color=00fff7>{0}</color> Points de récompense",
				["tempaddgroup"] = "Vous avez été ajouté à la temporalité <color=00fff7>{0}</color> groupe (expire dans {1})",
				["tempgrantperm"] = "Vous avez la temporalité accordée <color=00fff7>{0}</color> autorisation (expire dans {1})",
				["zlvl-wc"] = "Vous avez gagné <color=00fff7>{0}</color> niveau (x) de fabrication du bois",
				["zlvl-m"] = "Vous avez gagné <color=00fff7>{0}</color> niveau (x) d'extraction",
				["zlvl-s"] = "Vous avez gagné <color=00fff7>{0}</color> niveau (x) de skinning",
				["zlvl-c"] = "Vous avez gagné <color=00fff7>{0}</color> niveau (x) de fabrication",
				["zlvl-*"] = "Vous avez gagné <color=00fff7>{0}</color> à tous les niveaux",
				["oxidegrantperm"] = "Vous avez été accordé <color=00fff7>{0}</color> permission",
				["oxiderevokeperm"] = "Votre permission <color=00fff7>{0}</color> a été révoqué",
				["oxidegrantgroup"] = "Vous avez été ajouté à <color=00fff7>{0}</color> group",
				["oxiderevokegroup"] = "Vous avez été retiré de <color=00fff7>{0}</color> group"
            }, this, "fr");
			//Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["NoPermission"] = "¡No está autorizado a utilizar este comando!",
				["ClaimStatus"] = "[{0}] Verificado {1}, estado: {2}",
				["ClaimError"] = "¡Algo salió mal! Jugador <color=ff0000>{0} obtuvo un error</color> de <color=00fff7>{1}</color>. Por favor, inténtelo de nuevo más tarde!",
				["ClaimReward"] = "Acaba de recibir su (s) recompensa (s) de voto. ¡Disfrute!",
				["ClaimPleaseWait"] = "Verificación de los sitios web de votación. Espere ...",
				["VoteList"] = "Tu votaste <color=00fff7>{1}</color> hora!\n Deja otro voto en estos sitios:\n{0}",
				["EarnReward"] = "Cuando haya votado, escriba <color=00fff7>/claim</color> para reclamar tus recompensas!",
				["RewardListFirstTime"] = "Recompensa por votar por primera vez",
				["RewardListEverytime"] = "Recompensa, qué jugador recibirá cada vez que vote.",
				["RewardList"] = "Recompensa por votar <color=FFA500>{0}</color> hora.",
				["Received"] = "Ha recibido {0} x {1}",
				["ThankYou"] = "Gracias por votar! Tu votaste <color=00fff7>{0}</color> tiempo (s) Aquí está su recompensa por..\n{1}",
				["NoRewards"] = "No tienes nuevas recompensas disponibles\n Por favor escribe <color=00fff7>/vote</color> y vaya a uno de los sitios para votar y recibir su recompensa",
				["RemeberClaim"] = "¡Aún no ha reclamado su recompensa al votar por el servidor! utilizar <color=00fff7>/claim</color> ¡reclama tu recompensa!\n Debes reclamar tu recompensa en <color=00fff7>24h</color>! De lo contrario, se habrá ido!",
				["GlobalChatAnnouncments"] = "<color=00fff7>{0}</color> ha votado <color=00fff7>{1}</color> veces y acaba de recibir sus recompensas. Descubra dónde puede votar escribiendo<color=00fff7> /vote</color>\nPara ver una lista de recompensas disponibles<color=00fff7> /rewardlist</color>",
				["money"] = "<color=00fff7>{0}$</color> ha sido depositado en su cuenta",
				["rp"] = "Habéis ganado <color=00fff7>{0}</color> Puntos de recompensa",
				["tempaddgroup"] = "Has sido agregado a la temporalidad <color=00fff7>{0}</color> group (Expire in {1})",
				["tempgrantperm"] = "Tienes la temporalidad concedida <color=00fff7>{0}</color> permission (Expire in {1})",
				["zlvl-wc"] = "Has ganado <color= 0fff7> {0} </color> nivel (s) de artesanía en madera",
				["zlvl-m"] = "Has ganado <color=00fff7> {0} </color> nivel (s) de minería",
				["zlvl-s"] = "Has ganado <color=00fff7> {0} </color> nivel (s) de piel",
				["zlvl-c"] = "Has ganado <color=00fff7> {0} </color> nivel (s) de artesanía",
				["zlvl-*"] = "Habéis ganado <color=00fff7> {0} </color> a todos los niveles",
				["oxidegrantperm"] = "Se le ha concedido <color=00fff7> {0} </color> permiso",
				["oxiderevokeperm"] = "Su permiso <color=00fff7> {0} </color> ha sido revocado",
				["oxidegrantgroup"] = "Se le ha añadido al grupo <color=00fff7> {0} </color>",
				["oxiderevokegroup"] = "Se le ha eliminado del grupo <color=00fff7> {0} </color>"
            }, this, "es");
			//German
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["NoPermission"] = "Sie sind nicht berechtigt, diesen Befehl zu verwenden!",
				["ClaimStatus"] = "[{0}] Überprüft {1}, Status: {2}",
				["ClaimError"] = "Etwas ist schief gelaufen! Spieler <color=ff0000>{0} habe einen Fehler bekommen</color> von <color=00fff7>{1}</color>. Bitte versuchen Sie es später noch einmal!",
				["ClaimReward"] = "Sie haben gerade Ihre Abstimmungsbelohnung (en) erhalten. Genießen!",
				["ClaimPleaseWait"] = "Überprüfen der Abstimmungswebsites. Warten Sie mal...",
				["VoteList"] = "Du hast gewählt <color=00fff7>{1}</color> Zeit! \n Hinterlasse eine weitere Abstimmung auf diesen Seiten:\n{0}",
				["EarnReward"] = "Wenn Sie abgestimmt haben, geben Sie ein <color=00fff7>/claim</color> um deine Belohnungen zu beanspruchen!",
				["RewardListFirstTime"] = "Belohnung für die erste Abstimmung.",
				["RewardListEverytime"] = "Belohnung, welcher Spieler jedes Mal erhält, wenn er abstimmt.",
				["RewardList"] = "Belohnung für die Abstimmung <color=FFA500>{0}</color> Zeit.",
				["Received"] = "Sie haben {0} x {1} erhalten",
				["ThankYou"] = "Danke für Ihre Stimme! Du hast gewählt <color=00fff7>{0}</color> Zeit (en) Hier ist Ihre Belohnung für..\n{1}",
				["NoRewards"] = "Sie haben keine neuen Belohnungen verfügbar\n v <color=00fff7>/vote</color> und gehen Sie zu einer der Websites, um abzustimmen und Ihre Belohnung zu erhalten",
				["RemeberClaim"] = "Sie haben Ihre Belohnung noch nicht beansprucht, indem Sie für den Server gestimmt haben! verwenden <color=00fff7>/claim</color> rFordern Sie Ihre Belohnung an! \n Sie müssen Ihre Belohnung innerhalb beanspruchen <color=00fff7>24h</color>! Sonst wird es weg sein!",
				["GlobalChatAnnouncments"] = "<color=00fff7>{0}</color> gewählt <color=00fff7>{1}</color> mal und erhielt gerade ihre Belohnungen. Finden Sie heraus, wo Sie abstimmen können, indem Sie tippen<color=00fff7> /vote</color>\nEine Liste der verfügbaren Belohnungen anzeigen<color=00fff7> /rewardlist</color>",
				["money"] = "<color=00fff7>{0}$</color> wurde auf Ihr Konto eingezahlt",
				["rp"] = "Du hast gewonnen <color=00fff7>{0}</color> Belohnungspunkte",
				["tempaddgroup"] = "Sie wurden zur Zeitlichkeit hinzugefügt <color=00fff7>{0}</color> group (Expire in {1})",
				["tempgrantperm"] = "You have temporality granted <color=00fff7>{0}</color> permission (Expire in {1})",
				["zlvl-wc"] = "Sie haben <color=00fff7> {0} </color> Holzbearbeitungsstufe (n) gewonnen",
				["zlvl-m"] = "Sie haben <color=00fff7> {0} </color> Mining-Level (s) gewonnen",
				["zlvl-s"] = "Sie haben <color=00fff7> {0} </color> Skinning-Level (s) gewonnen",
				["zlvl-c"] = "Du hast <color=00fff7> {0} </color> Handwerksstufe (n) gewonnen",
				["zlvl-*"] = "Sie haben auf allen Ebenen <color=00fff7> {0} </color> gewonnen",
				["oxidegrantperm"] = "Ihnen wurde die Erlaubnis <color=00fff7> {0} </color> erteilt",
				["oxiderevokeperm"] = "Ihre Erlaubnis <color=00fff7> {0} </color> wurde widerrufen",
				["oxidegrantgroup"] = "Sie wurden zur Gruppe <color=00fff7> {0} </color> hinzugefügt",
				["oxiderevokegroup"] = "Sie wurden aus der <color=00fff7> {0} </color> -Gruppe entfernt."
            }, this, "de");
			//Russe
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["NoPermission"] = "У вас нет разрешения на использование этой команды!",
				["ClaimStatus"] = "<color=cyan>[{0}]</color> Checked {1}, Status: {2}",
				["ClaimError"] = "Что-то пошло не так! Игрок <color=ff0000>{0} получил ошибку</color> из <color=00fff7>{1}</color>. Пожалуйста, повторите попытку позже!",
				["ClaimReward"] = "Вы только что получили награды за голосование. Наслаждаться!",
				["ClaimPleaseWait"] = "Проверка сайтов для голосования. Подождите ...",
				["VoteList"] = "Вы проголосовали <color=00fff7>{1}</color> time(s)!\n Оставьте еще один голос на этих сайтах:\n{0}",
				["EarnReward"] = "Когда вы проголосовали, введите <color=00fff7>/claim</color> требовать свою награду (-а)!",
				["RewardListFirstTime"] = "<color=cyan>Награда за первое голосование.</color>",
				["RewardListEverytime"] = "<color=cyan>Награда, которую игрок будет получать каждый раз, когда проголосует.</color>",
				["RewardList"] = "<color=cyan>Награда за голосование</color> <color=FFA500>{0}</color> <color=cyan>time(s).</color>",
				["Received"] = "Вы получили {0} x {1}",
				["ThankYou"] = "Спасибо за ваш голос! Вы проголосовали <color=00fff7>{0}</color> раз (а) Вот ваша награда за ..\n{1}",
				["NoRewards"] = "У вас нет доступных новых наград\n Пожалуйста напечатайте <color=00fff7>/vote</color> и зайдите на один из сайтов, чтобы проголосовать и получить вознаграждение",
				["RemeberClaim"] = "Вы еще не забрали свою награду за голосование за сервер! Использовать <color=00fff7>/claim</color> получить награду!\n Вы должны получить свою награду в <color=00fff7>24h</color>! Иначе его не будет!",
				["GlobalChatAnnouncments"] = "<color=00fff7>{0}</color><color=cyan> проголосовал </color><color=00fff7>{1}</color><color=cyan> раз (а) и только что получили свои награды. Узнайте, где вы можете проголосовать, набрав</color><color=00fff7> /vote</color>\n<color=cyan> Чтобы увидеть список доступных типов наград</color><color=00fff7> /rewardlist</color>",
				["money"] = "<color=00fff7>{0}$</color> hбыл зачислен на ваш счет",
				["rp"] = "Вы получили <color=00fff7>{0}</color> Бонусные очки",
				["tempaddgroup"] = "Вы были временно добавлены в <color=00fff7>{0}</color> группа (срок действия истекает через {1})",
				["tempgrantperm"] = "Вам предоставлена временность <color=00fff7>{0}</color> разрешение (истекает через {1})",
				["zlvl-wc"] = "Вы получили <color=00fff7> {0} </color> уровень (-и) деревообработки ",
				["zlvl-m"] = "Вы достигли <color=00fff7> {0} </color> уровня (ов) добычи",
				["zlvl-s"] = "Вы получили <color=00fff7> {0} </color> уровень (и) скинов",
				["zlvl-c"] = "Вы получили <color=00fff7> {0} </color> уровень (-ы) изготовления",
				["zlvl-*"] = "Вы получили <color=00fff7>{0}</color> на всех уровнях",
				["oxidegrantperm"] = "Вам предоставлено <color=00fff7> {0} </color> разрешение ",
				["oxiderevokeperm"] = "Ваше разрешение <color=00fff7> {0} </color> отозвано ",
				["oxidegrantgroup"] = "Вы добавлены в группу <color=00fff7> {0} </color> ",
				["oxiderevokegroup"] = "Вы исключены из группы <color=00fff7> {0} </color> "
            }, this, "ru");
			//Turkish
            lang.RegisterMessages(new Dictionary<string, string>
            {
				["NoPermission"] = "Bu komutu kullanma yetkiniz yok! ",
				["ClaimStatus"] = "[{0}] Doğrulandı {1}, durum: {2}",
				["ClaimError"] = "Bir şeyler ters gitti! Oyuncu <color=ff0000>{0} <color=00fff7> {1} </color> ile </color> bir hata aldı. Lütfen daha sonra tekrar deneyiniz!",
				["ClaimReward"] = "Oylama ödüllerinizi az önce aldınız. Keyfini çıkarın!",
				["ClaimPleaseWait"] = "Oylama web sitelerinin doğrulaması. Lütfen bekleyin ...",
				["VoteList"] = "<color=00fff7> {1} </color> kez oy verdiniz! \n Bu sitelerde bir oy daha bırakın: \n {0}",
				["EarnReward"] = "Oy verdiğinizde, ödüllerinizi talep etmek için <color=00fff7> /request </color> girin!",
				["RewardListFirstTime"] = "İlk kez oy vermenin ödülü.",
				["RewardListEverytime"] = "Hangi oyuncunun her oy kullandığında alacağı ödül.",
				["RewardList"] = "<color=FFA500> {0} </color> zamanına oy vermenin ödülü.",
				["Received"] = "{0} x {1} aldınız",
				["ThankYou"] = "Oy verdiğiniz için teşekkür ederiz! <Color = 00fff7> {0} </color> kez oy verdiniz. İşte ödülünüz .. \n {1}",
				["NoRewards"] = "Kullanılabilir yeni ödülünüz yok \n Lütfen <color = 00fff7> /vote </color> girin ve oy vermek ve ödülünüzü almak için sitelerden birine gidin",
				["RemeberClaim"] = "Henüz sunucuya oy vererek ödülünüzü talep etmediniz! <Color=00fff7> /claim </color> kullanarak ödülünüzü talep edin! \n <color=00fff7> 24 saat içinde ödülünüzü talep etmelisiniz </color>! Aksi takdirde, gitmiş olacak! ",
				["GlobalChatAnnouncments"] = "<color = 00fff7> {0} </color>, <color = 00fff7> {1} </color> kez oy verdi ve ödüllerini aldı. <Color=00fff7> yazarak neff0000e oy verebileceğinizi öğrenin.  /vote </color> \nMevcut ödüllerin bir listesini görmek için <color=00fff7> /rewardlist </color> ",
				["money"] = "<color=00fff7> {0} $ </color> hesabınıza yatırıldı",
				["rp"] = "<color=00fff7> {0} </color> Ödül puanları kazandınız",
				["tempaddgroup"] = "<color=00fff7> {0} </color> grubuna eklendiniz (süresi {1} içinde sona erecek)",
				["tempgrantperm"] = "Geçici <color = 00fff7> {0} </color> yetkiniz var (süresi {1} içinde sona erecek)",
				["zlvl-wc"] = "Ağaç işleme <color = 00fff7> {0} </color> düzey (x) kazandınız",
				["zlvl-m"] = "<color=00fff7> {0} </color> madencilik seviyesi (x) kazandınız",
				["zlvl-s"] = "<color=00fff7> {0} </color> kaplama düzeyi (x) kazandınız",
				["zlvl-c"] = "<color=00fff7> {0} </color> üretim seviyesi (x) kazandınız",
				["zlvl-*"] = "Tüm seviyelerde <color=00fff7> {0} </color> kazandınız",
				["oxidegrantperm"] = "Size <color=00fff7> {0} </color> izni verildi",
				["oxiderevokeperm"] = "İzniniz <color=00fff7> {0} </color> iptal edildi",
				["oxidegrantgroup"] = "<color=00fff7> {0} </color> grubuna eklendiniz",
				["oxiderevokegroup"] = "<color=00fff7> {0} </color> grubundan çıkarıldınız"
            }, this, "tr");
        }
        #endregion

        #region Hooks
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // If (for some reason) player is stuck in claimCooldown list.
            if (claimCooldown.ContainsKey(player.userID))
                claimCooldown.Remove(player.userID);
        }

        private void SendHelpText(BasePlayer player)
        {
            // User
            if (hasPermission(player, permUse))
                player.ChatMessage(_helpYou.ToString());

            // Admin
            if (hasPermission(player, permAdmin))
                player.ChatMessage(_helpAdmin.ToString());
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!hasPermission(player, permUse))
                return;

            // Check if player exist in cooldown list or not
            if (!checkCooldown.ContainsKey(player.userID))
                checkCooldown.Add(player.userID, false);
            else if (checkCooldown.ContainsKey(player.userID))
                return;

            var timeout = 5500f; // Timeout (in milliseconds)

            foreach (var site in _availableAPISites.ToList())
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in _config.Servers)
                {
                    foreach (KeyValuePair<string, string> vp in kvp.Value)
                    {
                        if (vp.Key != site)
                            continue;

                        string[] idKeySplit = vp.Value.Split(':');
                        foreach (KeyValuePair<string, string> SitesApi in _config.VoteSitesAPI[site])
                        {
                            if (SitesApi.Key == PluginSettings.apiStatus)
                            {
                                // Formating api claim =>
                                // {0} = Key
                                // {1} PlayerID
                                string _format = String.Format(SitesApi.Value, idKeySplit[1], player.userID);

                                // Send GET request to voteAPI site.
                                webrequest.Enqueue(_format, null, (code, response) => CheckStatus(code, response, player), this, RequestMethod.GET, null, timeout);

                                _Debug($"GET: {_format} =>\n Site: {site} Server: {kvp.Key} Id: {idKeySplit[0]}");
                            }
                        }
                    }
                }
            }
            // Wait 3.69 sec before execute this command.
            // Because need make sure that plugin webrequest all api sites.
            timer.Once(3.69f, () =>
            {
                if (checkCooldown[player.userID])
                {
                    Chat(player, $"{_lang("RemeberClaim", player.UserIDString)}");
                }

                // Remove player from cooldown list
                checkCooldown.Remove(player.userID);
            });
        }
        #endregion

        #region Commands
        [ChatCommand("vote")]
        void cmdVote(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permUse))
            {
                Chat(player, _lang("NoPermission", player.UserIDString));
                return;
            }

            // Check how many time player has voted.
            int voted = 0;
            if (_storedData.Players.ContainsKey(player.UserIDString))
                voted = _storedData.Players[player.UserIDString].voted;

            Chat(player, _lang("VoteList", player.UserIDString, _voteList.ToString(), voted));
            Chat(player, _lang("EarnReward", player.UserIDString));
        }

        [ChatCommand("voteadmin")]
        void cmdVoteAdmin(BasePlayer player, string command, string[] args)
        {
            string errorString = "_ERROR_";
            if (!hasPermission(player, permAdmin))
                return;

            if (args?.Length < 1)
            {
                player.ChatMessage(_helpAdmin.ToString());
                return;
            }

            switch (args[0].ToLower())
            {
                default:
                    {
                        player.ChatMessage(_helpAdmin.ToString());
                    }
                    break;
                // voteadmin addvotepage (server name) (vote site) (ID) (KEY)
                case "addvotepage":
                case "addvote":
                    {
                        if (args.Length < 4)
                        {
                            StringBuilder _temp = new StringBuilder();

                            for (int i = 0; i < _availableAPISites.Count; i++)
                                _temp.AppendLine($" - [ID: {i}] {_availableAPISites[i]}");

                            Chat(player, $"USAGE: /voteadmin addvotepage (ServerName) (VoteSite ID) (API ID) (API KEY)\n"
                                 + $"VoteSite ID LIST:\n{_temp.ToString()}");
                            _temp.Clear();

                            return;
                        }

                        string serverName = (!string.IsNullOrEmpty(args[1]) ? args[1] : errorString);


                        string voteSite = string.Empty;
                        int voteSiteId = 0;
                        if (!int.TryParse(args[2], out voteSiteId))
                        {
                            Chat(player, "Use \"VoteSite ID\" to see all id(s) type /voteadmin delvote (ServerName).\nExample: /voteadmin delvote ServerName1");
                            return;
                        }

                        if (_availableAPISites.Count >= voteSiteId)
                            voteSite = _availableAPISites[voteSiteId];
                        else
                        {
                            Chat(player, "Oops, your Votesite ID was too high.");
                            return;
                        }

                        string voteID = (!string.IsNullOrEmpty(args[3]) ? args[3] : errorString);
                        string voteKEY = (!string.IsNullOrEmpty(args[4]) ? args[4] : errorString);
                        string IdKey = string.Empty;

                        if (voteID != errorString && voteKEY != errorString)
                            IdKey = $"{voteID}:{voteKEY}";
                        else
                            return; // TODO: Print error

                        PrintWarning($"[AddVotePage] Player: {player.displayName} => ServerName: {serverName}, VoteSite: {voteSite}, VoteID: {voteID}, VoteKEY: {voteKEY}");

                        try
                        {
                            bool succ = false;

                            if (!_config.Servers.ContainsKey(serverName))
                            {
                                _config.Servers.Add(serverName, new Dictionary<string, string>() { { voteSite, IdKey } });
                                succ = true;
                            }
                            else if (_config.Servers[serverName].ContainsKey(voteSite))
                            {
                                PrintWarning($"Replaced old {voteSite} value {_config.Servers[serverName][voteSite]} TO {IdKey}");
                                _config.Servers[serverName][voteSite] = IdKey;
                                succ = true;
                            }
                            else
                            {
                                _config.Servers[serverName].Add(voteSite, $"{voteID}:{voteKEY}");
                                succ = true;
                            }

                            if (succ)
                            {
                                _ReloadEasyVote();
                                Chat(player, $"Successfully added => ServerName: {serverName}, VoteSite: {voteSite}, VoteID: {voteID}, VoteKEY: {voteKEY}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Chat(player, "Oops, something went wrong. Check server console to see error message");
                            PrintError($"Error while tring save 'AddVotePage'\n{ex.ToString()}");
                        }
                    }
                    break;
                // voteadmin delvotepage (server name) (vote site)
                case "delvotepage":
                case "removevotepage":
                case "delvote":
                case "removevote":
                    {
                        if (args.Length <= 1)
                        {
                            StringBuilder _temp = new StringBuilder();

                            for (int i = 0; i < _availableAPISites.Count; i++)
                                _temp.AppendLine($" - [ID: {i}] {_availableAPISites[i]}");

                            Chat(player, $"USAGE: /voteadmin delvote (ServerName) [Optional: VoteSite ID]\n" +
                                $"- VoteSite ID is optional. If you do not add VoteSite id, then it will remove all vote sites inside that server.\n\n"
                                 + $"VoteSite ID LIST:\n{_temp.ToString()}");
                            _temp.Clear();

                            return;
                        }

                        string serverName = (!string.IsNullOrEmpty(args[1]) ? args[1] : errorString);
                        string voteSite = string.Empty;

                        if (args.Length < 2)
                        {
                            int voteSiteId = 0;
                            if (!int.TryParse(args[2], out voteSiteId))
                            {
                                Chat(player, "Use \"VoteSite ID\" to see all id(s) type /voteadmin delvote (ServerName).\nExample: /voteadmin delvote ServerName1");
                                return;
                            }

                            if (_availableAPISites.Count >= voteSiteId)
                                voteSite = _availableAPISites[voteSiteId];
                            else
                            {
                                Chat(player, "Oops, your Votesite ID was too high.");
                                return;
                            }
                        }

                        PrintWarning($"[DelVotePage] Player: {player.displayName} => ServerName: {serverName}" + (string.IsNullOrEmpty(voteSite) ? "" : $", VoteSite: {voteSite}"));

                        try
                        {
                            bool succ = false;

                            if (!_config.Servers.ContainsKey(serverName))
                                Chat(player, $"{serverName} does NOT exist.");
                            else if (!_config.Servers[serverName].ContainsKey(voteSite) && args.Length < 2)
                                Chat(player, $"{voteSite} does NOT exist in {serverName}.");
                            else
                            {
                                succ = true;
                                if (args.Length < 2)
                                {
                                    _config.Servers[serverName].Remove(voteSite);

                                    if (_config.Servers[serverName].Count == 0)
                                        _config.Servers.Remove(serverName);
                                }
                                else
                                {
                                    _config.Servers.Remove(serverName);
                                }
                            }

                            if (succ)
                            {
                                _ReloadEasyVote();
                                Chat(player, $"Successfully deleted {serverName}" + (string.IsNullOrEmpty(voteSite) ? "" : $" -> {voteSite}"));
                                //TODO: Add succs message to player
                            }
                        }
                        catch (Exception ex)
                        {
                            Chat(player, "Oops, something went wrong. Check server console to see error message");
                            PrintError($"Error while tring save 'DelVotePage'\n{ex.ToString()}");
                        }
                    }
                    break;
                // voteadmin addreward (reward number) (variables without spaces, split with , char)
                case "addreward":
                    {

                    }
                    break;
                // voteadmin servernames
                case "servernames":
                case "serverlist":
                case "servers":
                    {
                        StringBuilder _temp = new StringBuilder();

                        _temp.AppendLine("All current \"servers\" what you have added:");
                        foreach (var server in _config.Servers)
                        {
                            _temp.AppendLine($" - {server.Key}");
                            foreach (var servers in _config.Servers[server.Key])
                                _temp.AppendLine($"   * {servers.Key}");
                        }

                        Chat(player, _temp.ToString());
                    }
                    break;
                // voteadmin removereward (reward number)
                case "delreward":
                case "removereward":
                    {

                    }
                    break;
                // voteadmin editreward (reward number) TODO: PLAN THIS!
                case "editreward":
                    {

                    }
                    break;
                // voteadmin testreward
                case "testreward":
                case "test":
                        RewardHandler(player, null, true);
                    break;
                case "reload":
                    {
                        try
                        {
                            LoadConfigValues();
                            Chat(player, "Reloaded EasyVote configs successfully!");
                        }
                        catch (Exception ex)
                        {
                            Chat(player, "Oops, something went wrong. Check server console to see error message");
                            PrintError($"Error while tring 'reload'\n{ex.ToString()}");
                        }
                    }
                    break;
            }

            // TODO: Add admin commands
            // addserver <serverName>
            // removeserver <serverName>
            // addapi <serverName> <voteSitesApi> <id> <key>
            // removeapi <serverName> <voteSitesApi>
            // addreward <reward name (only number)> <commands in ">
            // removereward <reward name (only number)>
            // editreward <reward name (only number)> => Show voteX rewards => /voteadmin +"asdasdasd" -"asdasd"
            // showcmds 
            // testreward <reward name (only number)>
        }

        [ChatCommand("claim")]
        void cmdClaim(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permUse))
            {
                Chat(player, _lang("NoPermission", player.UserIDString));
                return;
            }

            // Check if player exist in cooldown list or not
            if (!claimCooldown.ContainsKey(player.userID))
                claimCooldown.Add(player.userID, new StringBuilder());
            else if (claimCooldown.ContainsKey(player.userID))
                return;

            var timeout = 5500f; // Timeout (in milliseconds)
            Chat(player, _lang("ClaimPleaseWait", player.UserIDString));

            foreach (var site in _availableAPISites.ToList())
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in _config.Servers)
                {
                    foreach (KeyValuePair<string, string> vp in kvp.Value)
                    {
                        // Make sure that key is site
                        if (vp.Key != site)
                            continue;

                        // Null check for ID & KEY
                        if (!vp.Value.Contains(":"))
                        {
                            _Debug($"{kvp.Key} {vp.Key} does NOT contains ID or Key !!!");
                            continue;
                        }
                        else if (vp.Value.Split(':')[0] == "ID")
                        {
                            _Debug($"{kvp.Key} {vp.Key} does NOT contains ID !!!");
                            continue;
                        }
                        else if (vp.Value.Split(':')[1] == "KEY")
                        {
                            _Debug($"{kvp.Key} {vp.Key} does NOT contains KEY !!!");
                            continue;
                        }

                        // Split ID & Key
                        string[] idKeySplit = vp.Value.Split(':');

                        // Loop API pages
                        foreach (KeyValuePair<string, string> SitesApi in _config.VoteSitesAPI[site])
                        {
                            // Got apiClaim url
                            if (SitesApi.Key == PluginSettings.apiClaim)
                            {
                                // Formating api claim =>
                                // {0} APIKey
                                // {1} SteamID
                                // Example: "http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key= {0} &steamid= {1} ",
                                string _format = String.Format(SitesApi.Value, idKeySplit[1], player.userID);

                                // Send GET request to voteAPI site.
                                webrequest.Enqueue(_format, null, (code, response) => ClaimReward(code, response, player, site, kvp.Key), this, RequestMethod.GET, null, timeout);

                                _Debug($"Player: {player.displayName} - Check claim URL: {_format}\nSite: {site} Server: {kvp.Key} VoteAPI-ID: {idKeySplit[0]} VoteAPI-KEY: {idKeySplit[1]}");
                            }
                        }
                    }
                }
            }

            // Wait 5.55 sec before remove player from cooldown list.
            timer.Once(5.55f, () =>
            {
                try
                {
                    // Print builded stringbuilder
                    Chat(player, claimCooldown[player.userID].ToString(), false);

                    // Remove player from cooldown list
                    claimCooldown.Remove(player.userID);
                }
                catch (Exception ex) { _Debug($"Error happen when try print \\claim status to \"{player.displayName}\"\n{ex.ToString()}"); PrintError("[ClaimStatus] Error printed to oxide/logs/EasyVote"); }
            });
        }

        [ChatCommand("reward")]
        void cmdReward(BasePlayer player, string command, string[] args)
        {
            if (!hasPermission(player, permUse))
            {
                Chat(player, _lang("NoPermission", player.UserIDString));
                return;
            }

            if (args?.Length > 1)
                return;

            if (args[0] == "list")
                rewardList(player);
        }
        #endregion

        #region Reward Handler
        private void RewardHandler(BasePlayer player, string serverName = null, bool adminTest = false)
        {
            // Check that player is in "database".
            var playerData = new PlayerData();
            if (!_storedData.Players.ContainsKey(player.UserIDString))
            {
                _storedData.Players.Add(player.UserIDString, playerData);
                _storedData.Players[player.UserIDString].lastTime_Voted = DateTime.UtcNow;
                Interface.GetMod().DataFileSystem.WriteObject("EasyVote", _storedData);
            }

            // Add +1 vote to player.
            _storedData.Players[player.UserIDString].voted++;
            _storedData.Players[player.UserIDString].lastTime_Voted = DateTime.UtcNow;
            Interface.GetMod().DataFileSystem.WriteObject("EasyVote", _storedData);

            // Get how many time player has voted.
            int voted = _storedData.Players[player.UserIDString].voted;

            // Take closest number from rewardNumbers
            int? closest = null;
            if (_numberMax.Count != 0)
            {
                try
                {
                    closest = (int?)_numberMax.Aggregate((x, y) => Math.Abs(x - voted) < Math.Abs(y - voted)
                            ? (x > voted ? y : x)
                            : (y > voted ? x : y));
                }
                catch (InvalidOperationException error) { _Debug($"Player {player.displayName} tried to claim a reward but this happened ...\n{error.ToString()}"); PrintError("[ClaimReward] Error printed to oxide/logs/EasyVote"); return; }

                if (closest > voted)
                {
                    _Debug($"Closest ({closest}) number was bigger then voted number ({voted}). Changed closest from ({closest}) to 0");
                    closest = 0;
                }

                _Debug($"Reward Number: {closest} Voted: {voted}");
            }

            // and here the magic happens. Loop for all rewards.
            foreach (KeyValuePair<string, List<string>> kvp in _config.Rewards)
            {
                // If first time voted
                if (kvp.Key.ToLower() == "first")
                {
                    // Make sure that this is player first time voting
                    if (voted > 1)
                        continue;

                    GaveRewards(player, kvp.Value);
                    continue;
                }

                // Gave this reward everytime
                if (kvp.Key == "@")
                {
                    GaveRewards(player, kvp.Value);
                    continue;
                }

                // Cumlative reward
                if (_config.Settings[PluginSettings.RewardIsCumulative].ToLower() == "true")
                {
                    if (kvp.Key.ToString().Contains("vote"))
                    {
                        // Tryparse vote number
                        int voteNumber;
                        if (!int.TryParse(kvp.Key.Replace("vote", ""), out voteNumber))
                            continue;

                        // All reward has now claimed
                        if (voteNumber > closest)
                            continue;

                        _Debug($" -> About to gave {kvp.Key} rewards");
                        GaveRewards(player, kvp.Value);
                    }
                    continue;
                }

                // Got closest vote
                if (closest != null)
                {
                    if (kvp.Key.ToString() == $"vote{closest}")
                    {
                        GaveRewards(player, kvp.Value);
                    }
                }

            }
            if (_config.Settings[PluginSettings.GlobalChatAnnouncments]?.ToLower() == "true" && !adminTest)
                PrintToChat($"{_lang("GlobalChatAnnouncments", player.UserIDString, player.displayName, voted)}");

            // Send message to discord text channel.
            if (_config.Discord[PluginSettings.DiscordEnabled].ToLower() == "true" && !adminTest)
            {
                List<Fields> fields = new List<Fields>();
                string json;

                fields.Add(new Fields("Voter", $"[{player.displayName}](https://steamcommunity.com/profiles/{player.userID})", true));
                fields.Add(new Fields("Voted", voted.ToString(), true));
                fields.Add(new Fields("Server", (serverName != null ? serverName : "[ UNKNOWN ]"), true));
                fields.Add(new Fields("Reward(s)", CleanHTML(rewardsString.ToString()), false));

                json = JsonConvert.SerializeObject(fields);
                DiscordMessages?.Call("API_SendFancyMessage", _config.Discord[PluginSettings.WebhookURL], _config.Discord[PluginSettings.Title], json, bool.Parse(_config.Discord[PluginSettings.Alert]) ? "@here" : null);
            }

            // Make sure that player has voted etc.
            if (rewardsString.Length > 1)
            {
                // Hookmethod: void onUserReceiveReward(BasePlayer player, int voted)
                Interface.CallHook("onUserReceiveReward", player, voted);

                // Send ThankYou to player
                if (_config.Settings[PluginSettings.LocalChatAnnouncments].ToLower() == "true")
                    Chat(player, $"{_lang("ThankYou", player.UserIDString, voted, rewardsString.ToString())}");

                // Clear rewardString
                rewardsString.Clear();
            }
        }

        /// <summary>
        /// Gave all rewards from List<string>
        /// </summary>
        /// <param name="player"></param>
        /// <param name="rewardValue"></param>
        private void GaveRewards(BasePlayer player, List<string> rewardValue)
        {
            foreach (string reward in rewardValue)
            {
                // Split reward to variable and value.
                string[] valueSplit = reward.Split(':');
                string commmand = valueSplit[0];
                string value = valueSplit[1].Replace(" ", "");

                // Checking variables and run console command.
                // If variable not found, then try give item.
                if (_config.Commands.ContainsKey(commmand))
                {
                    _Debug($"{getCmdLine(player, commmand, value)}");
                    rust.RunServerCommand(getCmdLine(player, commmand, value));

                    if (!value.Contains("-"))
                        rewardsString.AppendLine($"- {_lang(commmand, player.UserIDString, value)}");
                    else
                    {
                        string[] _value = value.Split('-');
                        rewardsString.AppendLine($"- {_lang(commmand, player.UserIDString, _value[0], _value[1])}");
                    }

                    _Debug($"Ran command {String.Format(commmand, value)}");
                    continue;
                }
                else
                {
                    try
                    {
                        Item itemToReceive = ItemManager.CreateByName(commmand, Convert.ToInt32(value));
                        _Debug($"Received item {itemToReceive.info.displayName.translated} x{value}");
                        //If the item does not end up in the inventory
                        //Drop it on the ground for them
                        if (!player.inventory.GiveItem(itemToReceive, player.inventory.containerMain))
                            itemToReceive.Drop(player.GetDropPosition(), player.GetDropVelocity());

                        rewardsString.AppendLine($"- {_lang("Received", player.UserIDString, value, itemToReceive.info.displayName.translated)}");
                    }
                    catch (Exception e) { PrintWarning($"{e}"); }
                }
            }
        }

        private string getCmdLine(BasePlayer player, string str, string value)
        {
            var output = String.Empty;
            string playerid = player.UserIDString;
            string playername = player.displayName;

            // Checking if value contains => -
            if (!value.Contains('-'))
                output = _config.Commands[str].ToString()
                    .Replace("{playerid}", playerid)
                    .Replace("{playername}", '"' + playername + '"')
                    .Replace("{value}", value);
            else
            {
                string[] splitValue = value.Split('-');
                output = _config.Commands[str].ToString()
                    .Replace("{playerid}", playerid)
                    .Replace("{playername}", '"' + playername + '"')
                    .Replace("{value}", splitValue[0])
                    .Replace("{value2}", splitValue[1]);
            }
            return $"{output}";
        }
        #endregion

        #region Storing
        class StoredData
        {
            public Dictionary<string, PlayerData> Players = new Dictionary<string, PlayerData>();
            public StoredData() { }
        }
        class PlayerData
        {
            public int voted;
            public DateTime lastTime_Voted;

            public PlayerData()
            {
                voted = 0;
                lastTime_Voted = DateTime.UtcNow;
            }
        }
        StoredData _storedData;
        #endregion

        #region Webrequests
        void ClaimReward(int code, string response, BasePlayer player, string url, string serverName = null)
        {
            _Debug($"URL: {url} - Code: {code}, Response: {response}");

            // Change response to number
            int responseNum = 0;
            if (!int.TryParse(response, out responseNum))
                _Debug($"Cant understand vote site {url} response \"{response}\"");

            // If vote site is down
            if (code != 200)
            {
                PrintError("Error: {0} - Couldn't get an answer for {1} ({2})", code, player.displayName, url);
                Chat(player, $"{_lang("ClaimError", player.UserIDString, code, url)}");
                return;
            }

            // Add response to StringBuilder
            if (claimCooldown.ContainsKey(player.userID))
            {
                claimCooldown[player.userID].AppendLine(_lang("ClaimStatus", player.UserIDString,
                    (!string.IsNullOrEmpty(serverName) ? serverName : string.Empty), url, $"<color={voteStatusColor[responseNum]}>{voteStatus[responseNum]}</color>"));
                //(!string.IsNullOrEmpty(serverName) ? $"<color=cyan>[{serverName}]</color> " : string.Empty)
                //+ $"Checked {url}, status: <color={voteStatusColor[responseNum]}>{voteStatus[responseNum]}</color>");
            }

            // If response is 1 = Voted & not yet claimed
            if (responseNum == 1)
                RewardHandler(player, serverName);
        }

        void CheckStatus(int code, string response, BasePlayer player)
        {
            _Debug($"Player: {player.displayName} - Code: {code}, Response: {response}");

            if (response?.ToString() == "1" && code == 200)
            {
                if (!checkCooldown.ContainsKey(player.userID))
                {
                    checkCooldown.Add(player.userID, true);
                }
                checkCooldown[player.userID] = true;
            }
        }
        #endregion

        #region Configuration

        #region Configuration Defaults
        PluginConfig DefaultConfig()
        {
            var defaultConfig = new PluginConfig
            {
                Settings = new Dictionary<string, string>
                {
                    { PluginSettings.Prefix, "<color=cyan>[EasyVote]</color>" },
                    { PluginSettings.RewardIsCumulative, "false" },
                    { PluginSettings.LogEnabled, "true" },
                    { PluginSettings.GlobalChatAnnouncments, "true" },
                    { PluginSettings.LocalChatAnnouncments, "true" }
                },
                Discord = new Dictionary<string, string>
                {
                    { PluginSettings.DiscordEnabled, "false" },
                    { PluginSettings.Alert, "false" },
                    { PluginSettings.Title, "Vote" },
                    { PluginSettings.WebhookURL, "" }
                },
                Servers = new Dictionary<string, Dictionary<string, string>> { },
                VoteSitesAPI = new Dictionary<string, Dictionary<string, string>>
                {
                    { "RustServers",
                       new Dictionary<string, string>() {
                           { PluginSettings.apiClaim, "http://rust-servers.net/api/?action=custom&object=plugin&element=reward&key={0}&steamid={1}" },
                           { PluginSettings.apiStatus, "http://rust-servers.net/api/?object=votes&element=claim&key={0}&steamid={1}" },
                           { PluginSettings.apiLink, "http://rust-servers.net/server/{0}" }
                       }
                    },
                    { "Beancan",
                       new Dictionary<string, string>() {
                           { PluginSettings.apiClaim, "http://beancan.io/vote/put/{0}/{1}" },
                           { PluginSettings.apiStatus, "http://beancan.io/vote/get/{0}/{1}" },
                           { PluginSettings.apiLink, "http://beancan.io/server/{0}" }
                       }
                    },
                },
                Rewards = new Dictionary<string, List<string>>
                {
                    { "first", new List<string>() { "oxidegrantperm: kits.starterkit" } },
                    { "@", new List<string>() { "supply.signal: 1", "zlvl-*: 1" } },
                    { "vote3", new List<string>() { "oxidegrantgroup: member" } },
                    { "vote6", new List<string>() { "money: 500", "tempaddgroup: vip-1d1h1m" } },
                    { "vote10", new List<string>() { "money: 1000", "rp: 50", "tempgrantperm: fauxadmin.allowed-5m" } }
                },
                Commands = new Dictionary<string, string>
                {
                    ["money"] = "deposit {playerid} {value}",
                    ["rp"] = "sr add {playerid} {value}",
                    ["oxidegrantperm"] = "oxide.grant user {playerid} {value}",
                    ["oxiderevokeperm"] = "oxide.revoke user {playerid} {value}",
                    ["oxidegrantgroup"] = "oxide.usergroup add {playerid} {value}",
                    ["oxiderevokegroup"] = "oxide.usergroup remove {playerid} {value}",
                    ["tempaddgroup"] = "addgroup {playerid} {value} {value2}",
                    ["tempgrantperm"] = "grantperm {playerid} {value} {value2}",
                    ["zlvl-c"] = "zl.lvl {playerid} C +{value}",
                    ["zlvl-wc"] = "zl.lvl {playerid} WC +{value}",
                    ["zlvl-m"] = "zl.lvl {playerid} M +{value}",
                    ["zlvl-s"] = "zl.lvl {playerid} S +{value}",
                    ["zlvl-*"] = "zl.lvl {playerid} * +{value}",
                }
            };
            return defaultConfig;
        }
        #endregion

        private bool configChanged;
        private PluginConfig _config;

        protected override void LoadDefaultConfig() => Config.WriteObject(DefaultConfig(), true);

        class PluginSettings
        {
            public const string apiClaim = "API Claim Reward (GET URL)";
            public const string apiStatus = "API Vote status (GET URL)";
            public const string apiLink = "Vote link (URL)";
            public const string Title = "Title";
            public const string WebhookURL = "Discord webhook (URL)";
            public const string DiscordEnabled = "DiscordMessage Enabled (true / false)";
            public const string Alert = "Enable @here alert (true / false)";
            public const string Prefix = "Prefix";
            public const string LogEnabled = "Enable logging => oxide/logs/EasyVote (true / false)";
            public const string RewardIsCumulative = "Vote rewards cumulative (true / false)";
            public const string GlobalChatAnnouncments = "Globally announcment in chat when player voted (true / false)";
            public const string LocalChatAnnouncments = "Send thank you message to player who voted (true / false)";
        }
        class PluginConfig
        {
            public Dictionary<string, string> Settings { get; set; }
            public Dictionary<string, string> Discord { get; set; }
            public Dictionary<string, Dictionary<string, string>> Servers { get; set; }
            public Dictionary<string, Dictionary<string, string>> VoteSitesAPI { get; set; }
            public Dictionary<string, List<string>> Rewards { get; set; }
            public Dictionary<string, string> Commands { get; set; }
        }
        void LoadConfigValues()
        {
            // Load config file
            _config = Config.ReadObject<PluginConfig>();
            var defaultConfig = DefaultConfig();

            try
            {
                // Try merge config
                Merge(_config.Settings, defaultConfig.Settings);
                Merge(_config.Discord, defaultConfig.Discord);
                Merge(_config.Servers, defaultConfig.Servers, true);
                Merge(_config.VoteSitesAPI, defaultConfig.VoteSitesAPI, true);
                Merge(_config.Rewards, defaultConfig.Rewards, true);
                Merge(_config.Commands, defaultConfig.Commands, true);
            }
            catch
            {
                // Print warning
                PrintWarning($"Could not read oxide/config/{Name}.json, creating new config file");

                // Load default config
                LoadDefaultConfig();
                _config = Config.ReadObject<PluginConfig>();

                // Merge config again
                Merge(_config.Settings, defaultConfig.Settings);
                Merge(_config.Discord, defaultConfig.Discord);
                Merge(_config.Servers, defaultConfig.Servers, true);
                Merge(_config.VoteSitesAPI, defaultConfig.VoteSitesAPI, true);
                Merge(_config.Rewards, defaultConfig.Rewards, true);
                Merge(_config.Commands, defaultConfig.Commands, true);
            }

            // If config changed, run this
            if (!configChanged) return;
            PrintWarning("Configuration file(s) updated!");
            Config.WriteObject(_config);
        }
        void Merge<T1, T2>(IDictionary<T1, T2> current, IDictionary<T1, T2> defaultDict, bool bypass = false)
        {
            foreach (var pair in defaultDict)
            {
                if (bypass) continue;
                if (current.ContainsKey(pair.Key)) continue;
                current[pair.Key] = pair.Value;
                configChanged = true;
            }
            var oldPairs = defaultDict.Keys.Except(current.Keys).ToList();
            foreach (var oldPair in oldPairs)
            {
                if (bypass) continue;
                current.Remove(oldPair);
                configChanged = true;
            }
        }
        #endregion

        #region Helper 
        public void Chat(BasePlayer player, string str, bool prefix = true) => SendReply(player, (prefix != false ? $"{_config.Settings["Prefix"]} " : string.Empty) + str);
        public void _Debug(string msg)
        {
            if (Convert.ToBoolean(_config.Settings[PluginSettings.LogEnabled]))
                LogToFile("EasyVote", $"[{DateTime.UtcNow.ToString()}] {msg}", this);

            if (DEBUG)
                Puts($"[Debug] {msg}");
        }

        private void HelpText()
        {
            _helpYou.Append("<color=cyan>EasyVote Commands ::</color>").AppendLine();
            _helpYou.Append("<color=yellow>/vote</color> - Show the voting website(s)").AppendLine();
            _helpYou.Append("<color=yellow>/claim</color> - Claim vote reward(s)").AppendLine();
            _helpYou.Append("<color=yellow>/reward list</color> - Display all reward(s) what you can get from voting.");

            _helpAdmin.Append("<color=cyan>EasyVote Admin Commands ::</color>").AppendLine();
            _helpAdmin.Append("<color=yellow>/voteadmin test</color> - Test reward(s)").AppendLine();
            _helpAdmin.Append("<color=yellow>/voteadmin addvotepage (ServerName) (VoteSite ID) (API ID) (API KEY)</color> - Add vote page. If ServerName does not exist, it will create it. VoteSite ID is (0: Beancan | 1: RustServers), if you just type /voteadmin addvotepage then id list will printed in chat.").AppendLine();
            _helpAdmin.Append("<color=yellow>/voteadmin delvotepage (ServerName) [Optional: VoteSite ID]</color> - Remove one (or if you leave VoteSite ID empty it will remove all vote pages) vote page(s)").AppendLine();
            _helpAdmin.Append("<color=yellow>/voteadmin servers</color> - List all vote page(s)").AppendLine();
            _helpAdmin.Append("<color=yellow>/voteadmin reload</color> - Reload config").AppendLine();
        }

        private string CleanHTML(string input)
        {
            return Regex.Replace(input, @"<(.|\n)*?>", string.Empty);
        }

        private bool hasPermission(BasePlayer player, string perm)
        {
            if (player.IsAdmin) return true;
            if (permission.UserHasPermission(player.UserIDString, perm)) return true;
            return false;
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        public class Fields
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }

        private void rewardList(BasePlayer player)
        {
            StringBuilder rewardList = new StringBuilder();

            int lineCounter = 0; // Count lines
            int lineSplit = 2; // Value when split reward list.

            foreach (KeyValuePair<string, List<string>> kvp in _config.Rewards)
            {
                if (kvp.Key.ToLower() == "first")
                {
                    rewardList.Append(_lang("RewardListFirstTime", null)).AppendLine();

                    var valueList = String.Join(Environment.NewLine, kvp.Value.ToArray());
                    rewardList.Append(valueList).AppendLine();
                    lineCounter++;
                }

                if (kvp.Key == "@")
                {
                    rewardList.Append(_lang("RewardListEverytime", null)).AppendLine();

                    var valueList = String.Join(Environment.NewLine, kvp.Value.ToArray());
                    rewardList.Append(valueList).AppendLine();
                    lineCounter++;
                }

                // If lineCounter is less then lineSplit.
                if (lineCounter <= lineSplit)
                {
                    int voteNumber;
                    if (!int.TryParse(kvp.Key.Replace("vote", ""), out voteNumber))
                    {
                        if (!(kvp.Key.ToLower() != "first" || kvp.Key.ToLower() != "@"))
                            PrintWarning($"[RewardHandler] Invalid vote config format \"{kvp.Key}\"");

                        continue;
                    }
                    rewardList.Append(_lang("RewardList", null, voteNumber)).AppendLine();

                    var valueList = String.Join(Environment.NewLine, kvp.Value.ToArray());
                    rewardList.Append(valueList).AppendLine();
                    lineCounter++;
                }
                // If higher, then send rewardList to player and empty it.
                else
                {
                    SendReply(player, rewardList.ToString());
                    rewardList.Clear();
                    lineCounter = 0;

                    int voteNumber;
                    if (!int.TryParse(kvp.Key.Replace("vote", ""), out voteNumber))
                    {
                        if (!(kvp.Key.ToLower() != "first" || kvp.Key.ToLower() != "@"))
                            PrintWarning($"[RewardHandler] Invalid vote config format \"{kvp.Key}\"");

                        continue;
                    }

                    rewardList.Append(_lang("RewardList", null, voteNumber)).AppendLine();
                    var valueList = String.Join(Environment.NewLine, kvp.Value.ToArray());
                    rewardList.Append(valueList).AppendLine();
                }
            }

            // This section is for making sure all rewards will be displayed.
            SendReply(player, rewardList.ToString());
        }

        private void BuildNumberMax()
        {
            foreach (KeyValuePair<string, List<string>> kvp in _config.Rewards)
            {
                // Ignore @ and first
                if (kvp.Key == "@")
                    continue;
                if (kvp.Key.ToLower() == "first")
                    continue;

                // If key contains "vote"
                if (kvp.Key.ToLower().Contains("vote"))
                {
                    int rewardNumber;

                    // Remove alphabetic and leave only number.
                    if (!int.TryParse(kvp.Key.Replace("vote", ""), out rewardNumber))
                    {
                        Puts($"Invalid vote config format \"{kvp.Key}\"");
                        continue;
                    }
                    _numberMax.Add(rewardNumber);
                }
            }
        }

        private void voteList()
        {
            List<string> temp = new List<string>();

            foreach (var site in _availableAPISites.ToList())
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in _config.Servers)
                {
                    foreach (KeyValuePair<string, string> vp in kvp.Value)
                    {
                        // Null checking
                        if (!vp.Value.Contains(":"))
                        {
                            _Debug($"{kvp.Key} {vp.Key} does NOT contains ID or Key !!!");
                            continue;
                        }
                        else if (vp.Value.Split(':')[0] == "ID")
                        {
                            _Debug($"{kvp.Key} {vp.Key} does NOT contains ID !!!");
                            continue;
                        }
                        else if (vp.Value.Split(':')[1] == "KEY")
                        {
                            _Debug($"{kvp.Key} {vp.Key} does NOT contains KEY !!!");
                            continue;
                        }

                        if (vp.Key == site)
                        {
                            string[] idKeySplit = vp.Value.Split(':');
                            foreach (KeyValuePair<string, string> SitesApi in _config.VoteSitesAPI[site])
                            {
                                if (SitesApi.Key == PluginSettings.apiLink)
                                {
                                    _Debug($"Added {String.Format(SitesApi.Value, idKeySplit[0])} to the stringbuilder list.");
                                    temp.Add($"<color=silver>{String.Format(SitesApi.Value, idKeySplit[0])}</color>");
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < temp.Count; i++)
            {
                _voteList.Append(temp[i]);
                if (i != (temp.Count - 1))
                    _voteList.AppendLine();
            }
        }

        private void checkVoteSites()
        {
            // Double check that VoteSitesAPI isnt null
            if (_config.VoteSitesAPI.Count == 0)
            {
                PrintWarning("VoteSitesAPI is null in oxide/config/EasyVote.json !!!");
                return;
            }

            // Add key names to List<String> availableSites
            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in _config.VoteSitesAPI)
            {
                bool pass = true;
                foreach (KeyValuePair<string, string> vp in kvp.Value)
                {
                    if (string.IsNullOrEmpty(vp.Value))
                    {
                        pass = false;
                        PrintWarning($"In '{kvp.Key}' value '{vp.Key}' is null (oxide/config/EasyVote.json). Disabled: {kvp.Key}");
                        continue;
                    }
                }

                if (pass)
                {
                    _Debug($"Added {kvp.Key} to the \"availableSites\" list");
                    _availableAPISites.Add(kvp.Key);
                }
            }
        }
        #endregion

        #region API
        // Output : string() UserID;
        // If there multiple player has same vote value, include them all in one string.
        // Multiple player output format: userid,userid,userid .. etc
        private string getHighestvoter()
        {
            // Helppers
            string output = string.Empty;
            int tempValue = 0;
            Dictionary<string, int> tempList = new Dictionary<string, int>();

            // Receive EasyVote StoreData and save it to tempList.
            foreach (KeyValuePair<string, PlayerData> item in _storedData.Players)
                tempList.Add(item.Key, item.Value.voted);

            // Loop tempList
            foreach (var item in tempList.OrderByDescending(key => key.Value))
            {
                // Run once
                if (tempValue == 0)
                {
                    output = item.Key;
                    tempValue = item.Value;
                    continue;
                }

                if (tempValue != 0)
                {
                    // If tempValue match.
                    if (item.Value == tempValue)
                    {
                        output += $",{item.Key}";
                    }
                    continue;
                }
            }
            return output;
        }

        // Output : string() UserID;
        private string getLastvoter()
        {
            string output = string.Empty;
            Dictionary<string, DateTime> tempList = new Dictionary<string, DateTime>();

            foreach (KeyValuePair<string, PlayerData> item in _storedData.Players)
                tempList.Add(item.Key, item.Value.lastTime_Voted);

            foreach (var item in tempList.OrderBy(x => x.Value).Take(1))
                output = item.Key;

            return output;
        }

        // Output : Only console message.
        private void resetPlayerVotedData(string steamID, bool displayMessage = true)
        {
            // Null checks
            if (string.IsNullOrEmpty(steamID))
                return;

            if (!_storedData.Players.ContainsKey(steamID))
                return;

            // Reset voted data
            int old = _storedData.Players[steamID].voted;
            _storedData.Players[steamID].voted = 0;
            Interface.GetMod().DataFileSystem.WriteObject("EasyVote", _storedData);

            // Print console message
            if (displayMessage)
                Puts($"Player '{steamID}' vote(s) data has been reseted from {old} to 0.");
        }

        // Output : Only console message.
        private void resetData(bool backup = true)
        {
            string currentTime = DateTime.UtcNow.ToString("dd-MM-yyyy");

            // Backup
            if (backup)
                Interface.GetMod().DataFileSystem.WriteObject($"EasyVote-{currentTime}.bac", _storedData);

            // Set new storedata
            _storedData = new StoredData();

            // Write wiped data
            Interface.GetMod().DataFileSystem.WriteObject("EasyVote", _storedData);

            Puts($"Storedata reseted, backup made in oxide/data/EasyVote-{currentTime}.bac");
        }
        #endregion
    }
}
