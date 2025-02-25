using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class LikeManager : StandardManager
    {
        public IMetaDataManager MetaDataManager { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataManager = core.Managers.MetaData;
            this.UserInterface = core.Components.UserInterface;
            base.InitializeComponent(core);
        }

        public async Task SetLike(IEnumerable<IFileData> fileDatas, bool like)
        {
            var count = fileDatas.Count();
            if (count > 1)
            {
                if (!this.UserInterface.Confirm(string.Format(Strings.LikeManager_Confirm, count)))
                {
                    return;
                }
            }
            foreach (var fileData in fileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    fileData.AddOrUpdate(CommonStatistics.Like, MetaDataItemType.Tag, Convert.ToString(like));
                }
            }
            await this.MetaDataManager.Save(
                fileDatas,
                new[] { CommonStatistics.Like },
                MetaDataUpdateType.User,
                MetaDataUpdateFlags.ShowReport | MetaDataUpdateFlags.RefreshHierarchies
            ).ConfigureAwait(false);
        }
    }
}
