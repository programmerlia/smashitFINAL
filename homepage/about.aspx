<%@ Page Language="C#" AutoEventWireup="true"
    MasterPageFile="~/homepage/homepage.Master"
    CodeBehind="about.aspx.cs"
    Inherits="Smash_IT.homepage.abouut" %>


<asp:Content ID="ContentHead" ContentPlaceHolderID="head" runat="server">

    <!-- Page-specific styles -->
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/hero.css") %>' />
    <link rel="stylesheet" href='<%= ResolveUrl("~/css/promos.css") %>' />

</asp:Content>


<asp:Content ID="ContentMain" ContentPlaceHolderID="MainContent" runat="server">

    <!-- HERO SECTION -->
    <section class="relative w-full h-[400px] md:h-[500px] flex items-center justify-center overflow-hidden">
        <div class="absolute inset-0 z-0">
            <img src="https://tse2.mm.bing.net/th/id/OIP.Pz1f1pfnzxUqQ_ugHB3pfAHaEK?rs=1&pid=ImgDetMain&o=7&rm=3"
                 class="w-full h-full object-cover" />
            <div class="absolute inset-0 bg-black/60"></div>
        </div>

        <div class="relative z-10 text-center px-4">
            <h1 class="text-5xl md:text-7xl font-bold text-white tracking-widest uppercase">
                About Us
            </h1>
            <div class="w-24 h-1 bg-[--primary-color] mx-auto mt-4 rounded-full"></div>
        </div>
    </section>


    <!-- OUR STORY -->
    <section class="py-16 md:py-24 max-w-7xl mx-auto px-6 md:px-12">
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">

            <div class="space-y-6">
                <h2 class="text-4xl md:text-5xl font-bold text-[#333]">Our Story</h2>

                <div class="text-lg text-[#555] leading-relaxed space-y-4">
                    <p>
                        It all started with a simple passion for the smash. Smash It began
                        as a small gathering of friends who just wanted a place to play,
                        unwind, and compete after work.
                    </p>

                    <p>
                        From humble beginnings in a rented community hall, we grew into a
                        full-fledged club dedicated to training champions.
                    </p>

                    <p>
                        Today, we continue to serve the community, fostering a family-like
                        environment.
                    </p>
                </div>
            </div>

            <div class="relative">
                <div class="absolute -inset-4 bg-[#82c9a8]/30 rounded-3xl transform rotate-2 -z-10 opacity-70"></div>

                <img src="https://static.wixstatic.com/media/11062b_4170afe661834b4eb9c4f763094172ad~mv2.jpg"
                     class="w-full rounded-2xl shadow-xl" />
            </div>

        </div>
    </section>


    <!-- MISSION / VISION -->
    <section class="relative py-24 bg-[#222] text-white">
        <div class="max-w-7xl mx-auto px-6 grid md:grid-cols-2 gap-16">

            <div class="border-l-4 border-[--primary-color] pl-6">
                <h3 class="text-3xl font-bold uppercase">Mission</h3>
                <p class="text-gray-300 text-lg">
                    To encourage physical fitness and provide exceptional service to our community.
                </p>
            </div>

            <div class="border-l-4 border-[--primary-color] pl-6">
                <h3 class="text-3xl font-bold uppercase">Vision</h3>
                <p class="text-gray-300 text-lg">
                    To be the premier badminton destination recognized for excellence.
                </p>
            </div>

        </div>
    </section>


    <!-- MEMBERSHIP -->
    <section class="bg-[#f9f9f9] py-20 text-center px-6">
        <h2 class="text-4xl font-bold text-[#333] mb-6">Membership</h2>

        <p class="text-xl mb-8">
            Join us to unlock full court access, training discounts, and tournaments.
        </p>

        <button class="bg-[#333] text-white px-8 py-3 rounded-full hover:bg-[--primary-color] hover:text-white transition">
            Join Now
        </button>
    </section>


    <!-- Page scripts (only if needed) -->
    <script src='<%= ResolveUrl("~/js/script.js") %>'></script>

</asp:Content>
