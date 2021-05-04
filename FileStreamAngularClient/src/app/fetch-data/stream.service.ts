import { Injectable } from '@angular/core';
import { Guid } from 'guid-typescript';

export class FileData {
  public filename: string;
  public chunkCount: number;
  public chunkIndex: number = 0;
  public size: number;
  public guid: string;
  public blob: File;
  public uploadRetries: number = 10;
}

@Injectable({
  providedIn: 'root'
})

export class StreamService {

  url: string = "/Streaming";
  chunkSize: number = (1 * 1024 * 1024);

  streamFile(file: File, filename: string) {
    var fileData = new FileData();
    fileData.guid = Guid.create().toString();
    fileData.filename = filename;
    fileData.chunkCount = Math.ceil(file.size / this.chunkSize);
    fileData.blob = file;
    return this.createChunk(fileData, 0);
  }
  createChunk(fileData: FileData, start){
    //We can add logic to handle multiple chunks AND multiple requests in here
    fileData.uploadRetries = 10;
    let end = Math.min(start + this.chunkSize, fileData.blob.size );
		const currentChunk = fileData.blob.slice(start, end);
		console.log(`Slice: ${++fileData.chunkIndex}/${fileData.chunkCount}`);
    const chunkForm = new FormData();
    chunkForm.append('file', currentChunk, fileData.filename);
		console.log(`Added file '${fileData.filename}' | Size ${currentChunk.size / 1024}KB`);
		this.uploadChunk(chunkForm, start, end, fileData);
  }

  uploadChunk(chunkForm, start, chunkEnd, fileData: FileData) {
    var xhr = new XMLHttpRequest();
    xhr.open("POST", this.url, true);
    var contentRange = "bytes " + start + "-" + chunkEnd + "/" + fileData.blob.size;
    xhr.setRequestHeader("Content-Range", contentRange);
    xhr.setRequestHeader("CorrelationId", fileData.guid);
    console.log(`added file guid '${fileData.guid}'`);	
    console.log("Content-Range", contentRange);

    xhr.onload = _ => {
      // Uploaded.
      console.log("uploaded chunk");
      console.log("oReq.response", xhr.response);
      start += this.chunkSize;
      if (start < fileData.blob.size) {
        this.createChunk(fileData, start);
      }
      else {
        console.log("all uploaded!");
      }

    };
    xhr.send(chunkForm);
    xhr.onabort = xhr.onerror = _ => {
      console.log(fileData.uploadRetries);
      //seems like there's and issue with angular proxy. 
      //This is a workaround to retry upload [{]fileData.uploadRetries] times before failing for testing purposes
      if(fileData.uploadRetries-- > 0)
        setTimeout(_=> this.uploadChunk(chunkForm, start, chunkEnd, fileData), 1000);
    };
  }
}
