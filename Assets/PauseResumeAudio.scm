AGSScriptModule        |  // Pause and Resume Audio Module by Laura Hunt

// AudioPriority and RepeatStyle must be set in the EDITOR for each audio clip

ChannelAttributes runningchannel[SYSTEMAUDIOCHANNELS];
export runningchannel;


function PauseAudio()
{    
  for (int i = 0; i < System.AudioChannelCount; i++) {
    AudioChannel* tempchannel = System.AudioChannels[i];
    if (tempchannel != null && tempchannel.IsPlaying) {
      runningchannel[i].channelvolume = tempchannel.Volume;
      runningchannel[i].channelpanning = tempchannel.Panning;
      runningchannel[i].channelposition = tempchannel.Position;
      runningchannel[i].channelclip = tempchannel.PlayingClip;
      runningchannel[i].channelspeed = tempchannel.Speed;
      tempchannel.Stop();
    }
  }
}


function ResumeAudio()
{
  for (int i = 0; i < System.AudioChannelCount; i++) {
    AudioChannel* tempchannel = System.AudioChannels[i];
    if (tempchannel != null) {
      AudioClip* tempclip = runningchannel[i].channelclip;
      if (tempclip != null) tempchannel = tempclip.Play(); // this will take whatever priority and repeat values are set in the editor
      tempchannel.Volume = runningchannel[i].channelvolume;
      tempchannel.Panning = runningchannel[i].channelpanning;
      tempchannel.Speed = runningchannel[i].channelspeed;
      tempchannel.Seek(runningchannel[i].channelposition);
    }
  }
}

 !  // Pause and Resume Audio Module by Laura Hunt

// AudioPriority and RepeatStyle must be set in the EDITOR for each audio clip

// change this value if the maximum number of channels is ever increased in a future version of AGS
#define SYSTEMAUDIOCHANNELS 8

struct ChannelAttributes
{
  int channelvolume;
  int channelpanning;
  int channelposition;
  int channelspeed;
  AudioClip * channelclip;
};

import ChannelAttributes runningchannel[SYSTEMAUDIOCHANNELS];
import function PauseAudio();
import function ResumeAudio();
 3ɩe        ej��