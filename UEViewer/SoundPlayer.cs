using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMOD;
using System.IO;
using System.Runtime.InteropServices;
namespace UEViewer
{
    public class SoundPlayer
    {
        private FMOD.Sound music = null, audioCoach = null;
        private FMOD.System system = null;
        private FMOD.Channel channel = null;
        private FMOD.RESULT result;
        public void Play(string filename)
        {
            int length;
            byte[] audiodata;
            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            audiodata = new byte[fs.Length];
            length = (int)fs.Length;
            fs.Read(audiodata, 0, length);
            fs.Close();
            this.Play(audiodata, length);

        }
        private void Init()
        {
            if (system == null)
            {
                result = FMOD.Factory.System_Create(ref system);
                ErrorCheck();
                result = system.init(1, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
                ErrorCheck();
            }
        }
        public void Play(byte[] audiodata, int length)
        {
            this.Init();
            FMOD.CREATESOUNDEXINFO exinfo = new FMOD.CREATESOUNDEXINFO();
            exinfo.cbsize = Marshal.SizeOf(exinfo);
            exinfo.length = (uint)length;
            result = system.createSound(audiodata, (FMOD.MODE.HARDWARE | FMOD.MODE.OPENMEMORY), ref exinfo, ref music);
            if (music != null)
            {
                result = this.music.setMode(FMOD.MODE.LOOP_OFF);
                ErrorCheck();
            }

            result = this.system.playSound(FMOD.CHANNELINDEX.FREE, music, false, ref channel);
            ErrorCheck();

        }
        public void getSpectrum(ref float[] spectrumarray, int numvalues, int channeloffset)
        {
            system.getSpectrum(spectrumarray, numvalues, channeloffset, DSP_FFT_WINDOW.TRIANGLE);
        }
        public void getWaveData(ref float[] wavearray, int numvalues, int channeloffset)
        {
            system.getWaveData(wavearray, numvalues, channeloffset);
        }

        private void ErrorCheck()
        {
            if (result != FMOD.RESULT.OK)
            {
                Console.WriteLine("FMOD error! " + result + " - " + GetError());
            }
        }
        public string GetError()
        {
            return FMOD.Error.String(result);
        }
    }

}
