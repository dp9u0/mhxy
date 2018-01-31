#pragma once
#include "dxsdk/include/d3d9.h"
#include "dxsdk/include/d3dx9.h"
#include <DSound.h>
#include <dshow.h>
#pragma comment(lib,"dxsdk/lib/x86/d3d9.lib")
#pragma comment(lib,"dxsdk/lib/x86/d3dx9.lib")
#pragma comment(lib,"dxsdk/lib/x86/dsound.lib")
#pragma comment(lib,"dxsdk/lib/x86/dxguid.lib")

using namespace std;
class cMySound
{
public:
	
	IGraphBuilder*      m_pGraphBuilder = NULL;
	IMediaControl*      m_pMediaControl = NULL;
	IMediaPosition*     m_pMediaPosition = NULL;
	IMediaEvent*    m_media_event = 0;
	void Reset();
	void Load(string path);
	void CheckEnd();
	void Free();
	~cMySound(){ Free(); };
private:
	string m_PrePath;

	

};


class cMyWav
{
public:
	 struct sWaveHeader {
		char  RiffSig[4];         // 'RIFF'
		long  WaveformChunkSize;  // 8
		char  WaveSig[4];         // 'WAVE'
		char  FormatSig[4];       // 'fmt ' (notice space after)
		long  FormatChunkSize;    // 16
		short FormatTag;          // WAVE_FORMAT_PCM
		short Channels;           // # of channels
		long  SampleRate;         // sampling rate
		long  BytesPerSec;        // bytes per second
		short BlockAlign;         // sample block alignment
		short BitsPerSample;      // bits per second
		char  DataSig[4];         // 'data'
		long  DataSize;           // size of waveform data
	} ;

	int m_size;
	BOOL bLoad = FALSE;
	LPDIRECTSOUNDBUFFER     m_pPrimaryBuffer=0;
	~cMyWav(){ Free(); };
	void Load(BYTE* pdata,int size,int pos=0,BOOL bPlay=TRUE);
	void Load2(BYTE* pdata, int size, int pos = 0, BOOL bPlay = TRUE);
	void Free();
	void SetPosition(int num);
	int GetNowPostion(){ return m_pPrimaryBuffer->GetCurrentPosition(0, 0); }
private:
};
