# Speech Recognition Service API Documentation

## Overview

This documentation covers the functionalities provided by the `ISpeechRecognitionService` interface, designed for handling speech recognition tasks. This service allows for asynchronous speech recording and processing, providing mechanisms to start and stop recordings, and to handle both real-time and post-processing recognition results.

## Interface: ISpeechRecognitionService

`ISpeechRecognitionService` provides methods for starting and stopping speech recordings, both individually and globally. It extends `IAsyncDisposable` for proper asynchronous resource management.

### Methods

#### StartRecordingAsync

Starts an asynchronous speech recording session with customizable options.

- **Signature 1:**
  ```c#
  Task<string> StartRecordingAsync(SpeechRecognitionOptions options, Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null);
  ```
  - `options`: Configuration settings for the speech recognition session.
  - `callback`: A method to handle the recognition results.
  - `stoppedCallback`: An optional method invoked when the recording stops.

- **Signature 2:**
  ```c#
  Task<string> StartRecordingAsync(Action<SpeechRecognitionResult> callback, Action<string> stoppedCallback = null);
  ```
  - `callback`: A method to handle the recognition results.
  - `stoppedCallback`: An optional method invoked when the recording stops.

#### StopRecordingAsync

Stops a specific ongoing speech recording.

- **Signature:**
  ```c#
  Task StopRecordingAsync(string recordingId);
  ```
  - `recordingId`: The identifier of the recording session to be stopped.

#### StopAllRecordingsAsync

Stops all ongoing speech recordings.

- **Signature:**
  ```c#
  Task StopAllRecordingsAsync();
  ```

## Class: SpeechRecognitionOptions

Configuration options for the speech recognition service.

### Properties

- `CultureInfo`: The culture information derived from the language setting, influencing recognition dialect and locale. Ignored during JSON serialization.
- `Lang`: The language code for the speech recognition.
- `FinalResultsOnly`: Specifies if only final recognition results should be returned.
- `Continuous`: Indicates whether the recognition service should continue listening after the user stops speaking.
