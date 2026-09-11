#if UNITY_EDITOR
using System.Collections.Generic;
using GeminiLab.Modules.RoomRelic;

namespace GeminiLab.Editor.SceneBootstrap
{
    /// <summary>2026-09 交流策划纸条；保留已有 ID 和 02 纸团类型，兼容旧存档。</summary>
    public static class ApartmentCommunicationContent
    {
        public static RoomNoteData[] CreateNotes()
        {
            var result = new List<RoomNoteData>();
            Add(result, "angel", "Angel", "Demon", new[]
            {
                "你的书掉在地上了，我放回去了。……顺便故意放反了一本。看看你多久能发现。",
                "记得喝水。只喝那些奇怪的饮料不算。关于这一点不要反驳我。",
                "我数了一下，你今天已经把东西随手丢了四次。第五次的时候，我决定不提醒你了。",
                "床边那只袜子……我本来决定当作没看见。后来还是把它踢远了一点。",
                "植物有点缺水，我替你浇了。放心，没有按照我的标准重新排列你的花。差一点而已。",
                "如果你在找那支笔，它滚到柜子下面去了。我其实十分钟前就看见了。",
                "房间今天很安静。难得。……安静太久之后，好像又有一点不习惯。",
                "桌上的零食放太久了，我已经替你“处理”掉了。",
                "你的画我没有动。不过……颜色搭得还不错。",
                // 原图此句末尾被裁切，仅补齐“划掉”标记的标点。
                "路过时听见你在玩游戏。（你怎么都不来找我玩——划掉）",
                "我把地上的纸捡起来了。有一张画得挺有意思，被我暂时没收。之后会还你的。",
                "你的贝斯放回架子上了。地上不是它该待的地方，虽然我知道你是故意乱放的。",
                "拨片掉在音响后面了，我替你拿出来放在桌上。下次可以不要什么都往角落里踢。",
                "音响旁边那几张纸我替你叠好了。放心，内容我没细看——虽然最上面那句确实很不像你会写的。",
                "我原本以为镜子是房间装饰。现在看来，它可能是你使用频率最高的家具。",
                "刚才那段很好听。虽然如果我现在夸你，你大概会得意一整天，所以当我没写。",
                "你弹贝斯的时候确实比平时安静一点。很神奇，明明房间反而更吵了。"
            });
            Add(result, "demon", "Demon", "Angel", new[]
            {
                "你的书摆得也太整齐了。看得我有点想故意抽歪一本。",
                "窗边晒太阳的位置还不错嘛。暂时征用了十分钟。",
                "你的房间还是这么亮。眼睛都快被闪到了。",
                "我本来想画点东西在这张纸上。算了，给你留个圈。◎",
                "床看起来挺舒服的。我只是评价一下，没躺。真的。",
                "今天什么恶作剧都没做。你最好珍惜。",
                "本来只是路过，结果门开着。门开着就是邀请，对吧？走的时候我帮你关好了，别想太多。",
                "你笔记里居然写了一个“？”——原来真的有连你也不知道的东西。稀奇。",
                "你那盆植物又往窗户那边歪了。看来它也知道哪里待着舒服。",
                "掉了一片叶子。我本来想扔掉，后来觉得还挺好看，给你夹书里了。",
                "我试着弹了两下你的竖琴。效果非常帅。可惜你没看到。算你的损失。",
                "本来想来捣乱的。结果坐下来以后突然懒得动了。今天算你走运，下次补上。",
                "你的书里夹着好多笔记。我本来只是随便看看……你为什么连吐槽都写得这么工整？",
                "刚才在窗边坐着坐着差点睡着。一定是你房间太无聊，不是因为这里很舒服。",
                "我发现你书架上居然有几本挺好看的。看来品味还不错嘛！",
                "你弹竖琴的时候确实挺像个天使的。",
                "这本我拿走了。想要回来？下次来我房间自己找。"
            });
            return result.ToArray();
        }

        private static void Add(List<RoomNoteData> notes, string prefix, string sender, string receiver, string[] contents)
        {
            for (int i = 0; i < contents.Length; i++)
                notes.Add(new RoomNoteData
                {
                    id = $"note_{prefix}_{i + 1:00}", senderCharacter = sender, receiverCharacter = receiver,
                    content = contents[i], weight = 1f,
                    visualType = i == 1 ? RoomNoteVisualType.PaperBall : RoomNoteVisualType.Note
                });
        }
    }
}
#endif
