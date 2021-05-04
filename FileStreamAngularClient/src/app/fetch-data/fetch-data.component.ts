import { Component, Inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { FileData, StreamService } from "./stream.service";

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})

export class FetchDataComponent {
  uploadProgress: Observable<FileData[]>;
  filenames: string[] = [];

  constructor(private uploadService: StreamService) {
  }

  upload(files: FileList) {
    for (let i = 0; i < files.length; i++) {
      this.filenames.push(files[i].name);
      console.log(`Uploading ${files[i].name} with size ${files[i].size} and type ${files[i].type}`);
      this.uploadService.streamFile(files[i], files[i].name);
    }}
}
