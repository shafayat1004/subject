/**
 * Platform constants and sample-cell selectors for gallery audits.
 */

export const PLATFORM = {
  WEB: 'web',
  ANDROID: 'android',
};

export const WEB_SAMPLE_CELL_SELECTOR =
  '.aesg-ContentComponent-table td.vertical-align-middle, .aesg-ContentComponent-table td.vertical-align-top';

/** React Native testId on ComponentSample visuals wrapper (RX.View.testId → testID). */
export const ANDROID_SAMPLE_CELL_SELECTOR = '~aesg-sample-visuals';

/**
 * @param {'web' | 'android'} platform
 */
export function sampleCellSelectorFor(platform) {
  return platform === PLATFORM.ANDROID ? ANDROID_SAMPLE_CELL_SELECTOR : WEB_SAMPLE_CELL_SELECTOR;
}

export const ANDROID_APP = {
  package: 'com.eggshell.appgallery',
  activity: 'com.eggshell.appgallery.MainActivity',
};
