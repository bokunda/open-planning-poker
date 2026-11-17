import React from 'react';
import { Composition } from 'remotion';
import { YouTube, YT_DURATION } from './YouTube';
import { Reels, R_DURATION } from './Reels';
import { YouTubeIntegrations, YTI_DURATION } from './YouTubeIntegrations';
import { ReelsIntegrations, RI_DURATION } from './ReelsIntegrations';
import { FPS } from './shared';

export const RemotionRoot: React.FC = () => (
  <>
    <Composition id="YouTube" component={YouTube} durationInFrames={YT_DURATION} fps={FPS} width={1920} height={1080} />
    <Composition id="Reels" component={Reels} durationInFrames={R_DURATION} fps={FPS} width={1080} height={1920} />
    <Composition id="YouTubeIntegrations" component={YouTubeIntegrations} durationInFrames={YTI_DURATION} fps={FPS} width={1920} height={1080} />
    <Composition id="ReelsIntegrations" component={ReelsIntegrations} durationInFrames={RI_DURATION} fps={FPS} width={1080} height={1920} />
  </>
);
