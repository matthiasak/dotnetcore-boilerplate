FROM microsoft/dotnet:latest

RUN apt-get -qq update && apt-get -qqy --no-install-recommends install git unzip
RUN curl -sL https://deb.nodesource.com/setup_6.x |  bash -
RUN apt-get install -y nodejs build-essential

LABEL name "dotnetcore-boilerplate"
RUN mkdir -p /app
COPY . /app
WORKDIR /app

RUN npm install -g yarn
RUN npm run setup

EXPOSE 5000/tcp
ENV ASPNETCORE_URLS http://*:5000

CMD ["npm", "run", "www"]