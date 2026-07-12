// Uploads a file to the .NET API's local storage and returns its public URL.
// folder must be one of: products | logos | certificates | qrcodes
import api from "./api";

export async function uploadFile(file, folder = "misc") {
  const formData = new FormData();
  formData.append("file", file);
  const { data } = await api.post(`/uploads?folder=${folder}`, formData, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return data.url;
}

export async function uploadMany(files, folder) {
  return Promise.all([...files].map((f) => uploadFile(f, folder)));
}
