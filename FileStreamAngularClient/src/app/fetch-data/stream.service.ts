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

  url: string = "/api/Streaming";
  chunkSize: number = (1 * 1024 * 1024 );
  chunksPerRequest: number = 5;

  streamFile(file: File, filename: string) {
    var fileData = new FileData();
    fileData.guid = Guid.create().toString();
    fileData.filename = filename;
    fileData.chunkCount = Math.ceil(file.size / this.chunkSize);
    fileData.blob = file;
    return this.createChunk(fileData, 0);
  }
  createChunk(fileData: FileData, start){
    fileData.uploadRetries = 10;
    const chunkForm = new FormData();
    let end = start;

    for(let i = 0; i < this.chunksPerRequest; i++){
      let chunkEnd = Math.min(end + this.chunkSize, fileData.blob.size );
      const currentChunk = fileData.blob.slice(end, chunkEnd);
      end = chunkEnd;

      console.log(`Slice: ${++fileData.chunkIndex}/${fileData.chunkCount}`);
      chunkForm.append('file', currentChunk, fileData.filename);
      console.log(`Added chunk | Size ${currentChunk.size / 1024}KB`);

      if(this.isFinished(fileData, chunkEnd)){
        console.log("Finished slicing file")
        break;
      }
    }
        
    console.log(`Added file '${fileData.filename}'`);
		this.uploadChunk(chunkForm, start, end, fileData);
  }

  uploadChunk(chunkForm, start, end, fileData: FileData) {
    var xhr = new XMLHttpRequest();
    xhr.open("POST", this.url, true);
    var contentRange = "bytes " + start + "-" + end + "/" + fileData.blob.size;
    xhr.setRequestHeader("Content-Range", contentRange);
    xhr.setRequestHeader("CorrelationId", fileData.guid);
    console.log(`File guid '${fileData.guid}'`);	
    console.log("Content-Range", contentRange);

    xhr.onload = _ => {
      // Uploaded.
      console.log("uploaded chunk");
      console.log("oReq.response", xhr.response);

      if (this.isFinished(fileData, end)) {
        console.log("all uploaded!");
      }
      else {
        this.createChunk(fileData, end);
      }

    };
    xhr.send(chunkForm);
    xhr.onabort = xhr.onerror = _ => {
      console.log(fileData.uploadRetries);
      //seems like there's and issue with angular proxy. 
      //This is a workaround to retry upload [{]fileData.uploadRetries] times before failing for testing purposes
      if(fileData.uploadRetries-- > 0)
        setTimeout(_=> this.uploadChunk(chunkForm, start, end, fileData), 1000);
    };
  }

  private isFinished(fileData: FileData, start: number) {
    return start >= fileData.blob.size;
  }
}
