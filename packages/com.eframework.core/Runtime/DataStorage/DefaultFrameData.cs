using System;
using UnityEngine;

namespace EFrameWork.Runtime.DataStorage
{
    public class DefaultFrameDataModal
    {
        public bool MusicOn;
        public long RegisterDate;
        public string RegisterVersion;
        public bool SoundOn;
        public float CurPlayTime; // 本次游戏时间(秒)
        public float TotalPlayTime; // 总游戏时间(秒)
        public bool VibrationOn;
    }

    public class DefaultFrameData : DataTable<DefaultFrameDataModal>
    {

        public bool VibrationOn
        {
            get => Data.VibrationOn;
            set
            {
                Data.VibrationOn = value;
                SetDirty();
            }
        }

        public bool MusicOn
        {
            get => Data.MusicOn;
            set
            {
                Data.MusicOn = value;
                SetDirty();
            }
        }

        public bool SoundOn
        {
            get => Data.SoundOn;
            set
            {
                Data.SoundOn = value;
                SetDirty();
            }
        }

        public long RegisterDate
        {
            get => Data.RegisterDate;
        }

        public string RegisterVersion
        {
            get => Data.RegisterVersion;
        }

        public float TotalPlayTime
        {
            get => Data.TotalPlayTime;
            set
            {
                Data.TotalPlayTime = value;
                SetDirty();
            }
        }
        
        public float CurPlayTime
        {
            get => Data.CurPlayTime;
            set
            {
                Data.CurPlayTime = value;
                SetDirty();
            }
        }

        // 重写 GetDefaultData 方法，提供默认值
        protected override DefaultFrameDataModal GetDefaultData()
        {
            return new DefaultFrameDataModal
            {
                MusicOn = true,
                RegisterDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                RegisterVersion = Application.version,
                SoundOn = true,
                TotalPlayTime = 0f,
                VibrationOn = true
            };
        }
    }
}
