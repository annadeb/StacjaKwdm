
function [currentFolder]=segment_tumor(folderPath,y,x,slice,outputPath)
[file_3D,metadata]=create_3Dimage(folderPath);
%imshow3D(BW);
P = RegionGrowingtemp(squeeze(file_3D), 70,[y,x,slice]);
K=imfill(P,8,'holes');
for i=1:size(K,3)
    Z=wljoin(double(int32(file_3D(:,:,i))),K(:,:,i),[1 1 0], 'be');
    Z1=Z(:,:,1);
    L(:,:,i)=Z1;
end
newStr = extractAfter(folderPath,"\\");

while(contains(newStr,'\\'))
newStr = extractAfter(newStr,"\\");
end
%mkdir(newStr);
cd(outputPath)
currentFolder=pwd;
for i=1:size(K,3)
    filename=char(string(num2str(i))+".dcm");
    metadata{i}.Filename='';
    s=struct;
    s.MediaStorageSOPClassUID=metadata{i}.MediaStorageSOPClassUID;
    s.MediaStorageSOPInstanceUID=metadata{i}.MediaStorageSOPInstanceUID;
    s.TransferSyntaxUID=metadata{i}.TransferSyntaxUID;
    s.ImplementationClassUID=metadata{i}.ImplementationClassUID;
    s.SOPClassUID=metadata{i}.SOPClassUID;
    s.SOPInstanceUID=metadata{i}.SOPInstanceUID;
    s.StudyInstanceUID=metadata{i}.StudyInstanceUID;
    s.SeriesInstanceUID=metadata{i}.SeriesInstanceUID;
    s.InstanceNumber=metadata{i}.InstanceNumber;

    dicomwrite(L(:,:,i),filename,s);
end
end