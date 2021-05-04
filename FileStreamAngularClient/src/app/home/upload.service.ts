import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { Upload } from 'tus-js-client';


export interface FileStatus {
  filename: string;
  progress: number;
  hash: string;
  guid: string;
  CorrelationId: string;
}

@Injectable({
  providedIn: 'root'
})

export class UploadService {

  private uploadStatus = new Subject<FileStatus[]>();
  uploadProgress = this.uploadStatus.asObservable();

  fileStatusArr: FileStatus[] = [];

  uploadFile(file: File, filename: string) {
    const fileStatus: FileStatus = { filename, progress: 0, hash: '', guid: '', CorrelationId: '' };
    this.fileStatusArr.push(fileStatus);

    this.uploadStatus.next(this.fileStatusArr);

    const upload = new Upload(file, {
      endpoint: "ByteArray",
      retryDelays: [0, 3000, 6000, 12000, 24000],
      chunkSize: 20000*1024,
      metadata: {
        filename,
        filetype: file.type
      },
      onError: async (error) => {
        console.log(error);
        return false;
      },
      onChunkComplete: (chunkSize, bytesAccepted, bytesTotal) => {
        this.fileStatusArr.forEach(value => {
          if (value.filename === filename) {
            value.progress = Math.floor(bytesAccepted / bytesTotal * 100);
            value.guid = upload.url.split('/').slice(-1)[0];
          }
        });
        this.uploadStatus.next(this.fileStatusArr);
      },
      onSuccess: async () => {
        this.fileStatusArr.forEach(value => {
          if (value.filename === filename) {
            value.progress = 100;
          }
        });
        this.uploadStatus.next(this.fileStatusArr);
        return true;
      }
    });
    upload.start();
  }
}
