using System;
using UnityEngine;

namespace EFrame.Runtime.DataStorage
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
        public const string TableStorageKey = "default-frame-data";

        protected override int CurrentVersion => 1;

        protected override string GetStorageKey()
        {
            return TableStorageKey;
        }

        public bool VibrationOn
        {
            get => Data.VibrationOn;
            set => SetValue(ref m_data.VibrationOn, value);
        }

        public bool MusicOn
        {
            get => Data.MusicOn;
            set => SetValue(ref m_data.MusicOn, value);
        }

        public bool SoundOn
        {
            get => Data.SoundOn;
            set => SetValue(ref m_data.SoundOn, value);
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
            set => SetValue(ref m_data.TotalPlayTime, value);
        }
        
        public float CurPlayTime
        {
            get => Data.CurPlayTime;
            set => SetValue(ref m_data.CurPlayTime, value);
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

        protected override DefaultFrameDataModal Migrate(DefaultFrameDataModal data, int fromVersion)
        {
            if (data == null)
            {
                return GetDefaultData();
            }

            if (fromVersion < 1)
            {
                if (data.RegisterDate <= 0)
                {
                    data.RegisterDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }

                if (string.IsNullOrEmpty(data.RegisterVersion))
                {
                    data.RegisterVersion = Application.version;
                }
            }

            return data;
        }
    }
}
