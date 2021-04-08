% folderPath1 = actualFile folder
function [file_3D,metadata]=create_3Dimage(folderPath1)
listing=dir(folderPath1);
listing=listing([listing.isdir]==0);


for i =1:length(listing)
    Fileaddress{i,1}=strcat(folderPath1,'\',listing(i).name);
    
    info=dicominfo(Fileaddress{i,1});
    file_3D(:,:,info.InstanceNumber)=dicomread(Fileaddress{i,1});
    metadata{info.InstanceNumber}=info;
end
end